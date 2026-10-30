// D-025: cloud save through iCloud key-value storage. No login screen and no
// server: iOS syncs the value with the player's iCloud account, keeps writes
// locally while offline and delivers them once the network is back.
@import Foundation;

extern void UnitySendMessage(const char *obj, const char *method, const char *msg);

static NSString *const SaveKey = @"TreadShredSave";
static id changeObserver;

// The stored save as UTF-8 JSON (empty when none); Unity frees the copy.
const char *TreadShredCloudLoad(void)
{
    NSString *json = [NSUbiquitousKeyValueStore.defaultStore stringForKey:SaveKey] ?: @"";
    return strdup(json.UTF8String);
}

void TreadShredCloudStore(const char *json)
{
    NSUbiquitousKeyValueStore *store = NSUbiquitousKeyValueStore.defaultStore;
    [store setString:[NSString stringWithUTF8String:json] forKey:SaveKey];
    [store synchronize];
}

// Tells Unity (OnCloudChanged) when another device or the first sync after an install changes the save.
void TreadShredCloudWatch(void)
{
    if (changeObserver != nil)
        return;
    NSUbiquitousKeyValueStore *store = NSUbiquitousKeyValueStore.defaultStore;
    changeObserver = [NSNotificationCenter.defaultCenter
        addObserverForName:NSUbiquitousKeyValueStoreDidChangeExternallyNotification
                    object:store
                     queue:NSOperationQueue.mainQueue
                usingBlock:^(NSNotification *note) {
        NSNumber *reason = note.userInfo[NSUbiquitousKeyValueStoreChangeReasonKey];
        NSLog(@"[TreadShred] iCloud save changed externally (reason %@)", reason);
        UnitySendMessage("TreadShredCloudSave", "OnCloudChanged", "");
    }];
    [store synchronize];
}
