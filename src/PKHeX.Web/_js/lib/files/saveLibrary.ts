/*
    Save library: link a folder on disk once, then find the saves inside it on every later visit.

    Notes that shaped this file, all verified against the spec rather than assumed:

    - A FileSystemDirectoryHandle is structured-cloneable and may be stored in IndexedDB, but the
      permission grant is NOT stored with it. A restored handle usually reports "prompt".
    - requestPermission() needs transient activation, so it can only run from a real user gesture.
    - Directory enumeration is one level at a time, so the recursion lives here rather than crossing
      the interop boundary once per entry.
    - Chromium writes in-progress files as a sibling "<name>.crswap" in the user's own folder, so those
      have to be skipped or the scan offers half-written files as loadable saves.
    - This owns its own IndexedDB database. The app's existing TG.Blazor.IndexedDB store is driven
      through JSON serialisation, which cannot carry a handle at all, and sharing its database would
      tangle two version lifecycles.
*/

const DB_NAME = "pkhex-save-library";
const DB_VERSION = 1;
const STORE = "handles";
const ROOT_KEY = "root-folder";

export type ScannedFile = {
    id: string;
    name: string;
    path: string;
    size: number;
    lastModified: number;
};

export type FolderState = {
    name: string;
    permission: PermissionState | "unsupported";
};

let rootHandle: any = null;

/// Handles stay on this side; .NET addresses files by opaque id so no per-file reference is pinned
/// across the interop boundary.
const fileHandles = new Map<string, any>();

function openDatabase(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = () => {
            if (!request.result.objectStoreNames.contains(STORE)) {
                request.result.createObjectStore(STORE);
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function putHandle(handle: any): Promise<void> {
    const db = await openDatabase();
    await new Promise<void>((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        tx.objectStore(STORE).put(handle, ROOT_KEY);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
    db.close();
}

async function getHandle(): Promise<any> {
    const db = await openDatabase();
    const handle = await new Promise<any>((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const request = tx.objectStore(STORE).get(ROOT_KEY);
        request.onsuccess = () => resolve(request.result ?? null);
        request.onerror = () => reject(request.error);
    });
    db.close();
    return handle;
}

async function clearHandle(): Promise<void> {
    const db = await openDatabase();
    await new Promise<void>((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        tx.objectStore(STORE).delete(ROOT_KEY);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
    db.close();
}

/// FileSystemDirectoryHandle and createWritable also exist in Firefox and Safari via the Origin Private
/// File System, so neither can be used to detect this. showDirectoryPicker plus queryPermission can.
export function isSaveLibrarySupported(): boolean {
    const anyWindow = window as any;
    return typeof anyWindow.showDirectoryPicker === "function"
        && typeof anyWindow.FileSystemHandle?.prototype?.queryPermission === "function";
}

async function permissionOf(handle: any): Promise<PermissionState | "unsupported"> {
    if (!handle || typeof handle.queryPermission !== "function") return "unsupported";
    return await handle.queryPermission({mode: "read"});
}

export type PickResult = {
    status: "ok" | "cancelled" | "unsupported" | "failed";
    folder: FolderState | null;
    message: string;
};

export async function pickSaveFolder(): Promise<PickResult> {
    if (!isSaveLibrarySupported()) {
        return {status: "unsupported", folder: null, message: ""};
    }

    let handle: any;
    try {
        // "read" for now: this build only reads. Asking for readwrite before anything can write would be
        // asking the user to trust us with their saves earlier than we have earned it.
        handle = await (window as any).showDirectoryPicker({
            id: "pkhex-save-library",
            mode: "read",
            startIn: "documents",
        });
    } catch (error: any) {
        // The browser refuses some directories outright - on Linux everything under ~/.config, which is
        // exactly where RetroArch keeps its saves. Cancelling that dialog arrives here as AbortError,
        // indistinguishable from an ordinary cancel, so the page offers the workaround either way.
        const name = error?.name ?? "";
        if (name === "AbortError") {
            return {status: "cancelled", folder: null, message: ""};
        }
        return {status: "failed", folder: null, message: error?.message ?? String(error)};
    }

    if (!handle) return {status: "cancelled", folder: null, message: ""};

    rootHandle = handle;
    await putHandle(handle);

    return {
        status: "ok",
        folder: {name: handle.name, permission: await permissionOf(handle)},
        message: "",
    };
}

export async function restoreSaveFolder(): Promise<FolderState | null> {
    if (!isSaveLibrarySupported()) return null;

    const handle = await getHandle();
    if (!handle) return null;

    rootHandle = handle;
    return {name: handle.name, permission: await permissionOf(handle)};
}

/// Must be called from a user gesture - the spec rejects a prompt without transient activation.
export async function requestSaveFolderPermission(): Promise<PermissionState | "unsupported"> {
    if (!rootHandle || typeof rootHandle.requestPermission !== "function") return "unsupported";

    try {
        return await rootHandle.requestPermission({mode: "read"});
    } catch {
        return "denied";
    }
}

export async function forgetSaveFolder(): Promise<void> {
    rootHandle = null;
    fileHandles.clear();
    await clearHandle();
}

function isIgnored(name: string): boolean {
    const lower = name.toLowerCase();
    return lower.endsWith(".crswap")       // Chromium's own in-progress write file
        || lower.endsWith(".bak")
        || lower.startsWith(".");
}

export async function scanSaveFolder(maxDepth: number): Promise<ScannedFile[]> {
    if (!rootHandle) return [];

    fileHandles.clear();
    const found: ScannedFile[] = [];
    let nextId = 0;

    async function walk(directory: any, prefix: string, depth: number): Promise<void> {
        if (depth > maxDepth) return;

        for await (const entry of directory.values()) {
            if (isIgnored(entry.name)) continue;

            const path = prefix ? `${prefix}/${entry.name}` : entry.name;

            if (entry.kind === "directory") {
                await walk(entry, path, depth + 1);
                continue;
            }

            const file = await entry.getFile();
            const id = `f${nextId++}`;
            fileHandles.set(id, entry);
            found.push({
                id,
                name: entry.name,
                path,
                size: file.size,
                lastModified: file.lastModified,
            });
        }
    }

    await walk(rootHandle, "", 0);
    return found;
}

export async function readSaveFile(id: string): Promise<Uint8Array | null> {
    const handle = fileHandles.get(id);
    if (!handle) return null;

    const file = await handle.getFile();
    return new Uint8Array(await file.arrayBuffer());
}
