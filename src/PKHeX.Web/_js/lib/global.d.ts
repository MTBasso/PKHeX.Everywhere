import {decryptAes, encryptAes} from "./crypto/aes.ts";
import {md5Hash} from "./crypto/md5.ts";
import {downloadFileFromStream} from "./files/files.ts";
import {setShellAttribute, setStylesheetEnabled} from "./ui/shell.ts";
import {
    forgetSaveFolder,
    isSaveLibrarySupported,
    pickSaveFolder,
    readSaveFile,
    requestSaveFolderPermission,
    restoreSaveFolder,
    scanSaveFolder,
} from "./files/saveLibrary.ts";
import {User} from "./types.ts";

type IdToken = string;

declare global {
    interface Window {
        // crypt
        encryptAes: typeof encryptAes;
        decryptAes: typeof decryptAes;
        md5Hash: typeof md5Hash;

        // files
        downloadFileFromStream: typeof downloadFileFromStream;

        // save library
        isSaveLibrarySupported: typeof isSaveLibrarySupported;
        pickSaveFolder: typeof pickSaveFolder;
        restoreSaveFolder: typeof restoreSaveFolder;
        requestSaveFolderPermission: typeof requestSaveFolderPermission;
        forgetSaveFolder: typeof forgetSaveFolder;
        scanSaveFolder: typeof scanSaveFolder;
        readSaveFile: typeof readSaveFile;
        
        // ui shell
        setShellAttribute: typeof setShellAttribute;
        setStylesheetEnabled: typeof setStylesheetEnabled;

        // ui
        getWidth: () => number;
        hasPreferenceForDarkTheme: () => boolean;
        clickElement: (element: HTMLElement | null | undefined) => void;
        
        // firebase
        isFirebaseAuthEnabled: () => boolean;
        isSignedIn: () => boolean;
        getAuthToken: () => Promise<IdToken>;
        signInAnonymously: () => Promise<IdToken>;
        getSignedInUser: () => User | null;
        signOut: () => Promise<void>;
    }
}