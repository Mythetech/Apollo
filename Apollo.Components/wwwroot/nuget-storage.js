const DB_VERSION = 1;

function openDatabase(dbName) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, DB_VERSION);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
        
        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            
            if (!db.objectStoreNames.contains('packages')) {
                db.createObjectStore('packages', { keyPath: 'packageId' });
            }
            
            if (!db.objectStoreNames.contains('assemblies')) {
                db.createObjectStore('assemblies');
            }
        };
    });
}

export async function storePackageMetadata(dbName, storeName, packageData) {
    const db = await openDatabase(dbName);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.put(packageData);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve();
        tx.oncomplete = () => db.close();
    });
}

export async function storeAssemblyData(dbName, storeName, key, data) {
    const db = await openDatabase(dbName);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.put(data, key);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve();
        tx.oncomplete = () => db.close();
    });
}

export async function getAssemblyData(dbName, storeName, key) {
    const db = await openDatabase(dbName);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.get(key);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result || null);
        tx.oncomplete = () => db.close();
    });
}

export async function getAllPackages(dbName, storeName) {
    const db = await openDatabase(dbName);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.getAll();
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result || []);
        tx.oncomplete = () => db.close();
    });
}

export async function getPackage(dbName, storeName, packageId) {
    const db = await openDatabase(dbName);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.get(packageId);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result || null);
        tx.oncomplete = () => db.close();
    });
}

export async function isPackageInstalled(dbName, storeName, packageId, version) {
    const pkg = await getPackage(dbName, storeName, packageId);
    if (!pkg) return false;
    if (version && pkg.version !== version) return false;
    return true;
}

export async function removePackage(dbName, storeName, packageId) {
    const db = await openDatabase(dbName);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.delete(packageId);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve();
        tx.oncomplete = () => db.close();
    });
}

export async function removeAssemblyData(dbName, storeName, key) {
    const db = await openDatabase(dbName);
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.delete(key);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve();
        tx.oncomplete = () => db.close();
    });
}

