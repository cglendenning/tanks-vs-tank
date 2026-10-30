// D-032: the Game Center leaderboard through GameKit directly. Unity's
// built-in Game Center bridge froze the game when a signed-out player tapped
// LEADERBOARD, because it asked GameKit to sign in again and waited for an
// answer GameKit gives only once per launch. Here nothing pauses Unity, and
// Apple's sign-in screen is kept until the player asks for it.
@import GameKit;
@import UIKit;

extern void UnitySendMessage(const char *obj, const char *method, const char *msg);
extern UIViewController *UnityGetGLViewController(void);

static const char *Receiver = "TreadShredGameCenter";
static UIViewController *signInController;
static BOOL handlerInstalled;

static void Send(const char *method, NSString *message)
{
    UnitySendMessage(Receiver, method, message.UTF8String);
}

static BOOL Present(UIViewController *controller)
{
    UIViewController *root = UnityGetGLViewController();
    if (root == nil || root.presentedViewController != nil)
        return NO;
    [root presentViewController:controller animated:YES completion:nil];
    return YES;
}

// Starts sign-in without showing anything; reports "1", "0" or "offer" to OnSignIn.
void TreadShredGameCenterSignIn(void)
{
    if (handlerInstalled)
    {
        Send("OnSignIn", GKLocalPlayer.localPlayer.isAuthenticated ? @"1" : @"0");
        return;
    }
    handlerInstalled = YES;
    GKLocalPlayer.localPlayer.authenticateHandler = ^(UIViewController *controller, NSError *error) {
        if (controller != nil)
        {
            signInController = controller;
            Send("OnSignIn", @"offer");
            return;
        }
        if (error != nil)
            NSLog(@"[TreadShred] Game Center sign-in failed: %@", error.localizedDescription);
        Send("OnSignIn", GKLocalPlayer.localPlayer.isAuthenticated ? @"1" : @"0");
    };
}

int TreadShredGameCenterIsSignedIn(void)
{
    return GKLocalPlayer.localPlayer.isAuthenticated ? 1 : 0;
}

// Shows Apple's sign-in screen if GameKit offered one; 0 means the player must sign in from iOS Settings.
int TreadShredGameCenterOfferSignIn(void)
{
    if (signInController == nil)
        return 0;
    UIViewController *controller = signInController;
    signInController = nil;
    return Present(controller) ? 1 : 0;
}

void TreadShredGameCenterSubmit(long long score, const char *board)
{
    NSString *boardId = [NSString stringWithUTF8String:board];
    NSLog(@"[TreadShred] Game Center submitting %lld to %@", score, boardId);
    [GKLeaderboard submitScore:(NSInteger)score context:0 player:GKLocalPlayer.localPlayer
                leaderboardIDs:@[boardId] completionHandler:^(NSError *error) {
        if (error != nil)
            NSLog(@"[TreadShred] Game Center score submit failed: %@", error.localizedDescription);
        else
            NSLog(@"[TreadShred] Game Center accepted %lld", score);
        Send("OnSubmitted", error == nil ? @"1" : @"0");
    }];
}

void TreadShredGameCenterLoadRank(const char *board)
{
    NSString *boardId = [NSString stringWithUTF8String:board];
    [GKLeaderboard loadLeaderboardsWithIDs:@[boardId] completionHandler:^(NSArray<GKLeaderboard *> *boards, NSError *error) {
        GKLeaderboard *leaderboard = boards.firstObject;
        if (leaderboard == nil)
        {
            NSLog(@"[TreadShred] Game Center leaderboard not loaded: %@", error.localizedDescription);
            Send("OnRank", @"0:0");
            return;
        }
        [leaderboard loadEntriesForPlayerScope:GKLeaderboardPlayerScopeGlobal timeScope:GKLeaderboardTimeScopeAllTime
                                         range:NSMakeRange(1, 1)
                             completionHandler:^(GKLeaderboardEntry *local, NSArray<GKLeaderboardEntry *> *entries,
                                                 NSInteger total, NSError *loadError) {
            if (loadError != nil)
                NSLog(@"[TreadShred] Game Center player entry not loaded: %@", loadError.localizedDescription);
            // "rank:score" of the player's own entry; "0:0" when Game Center holds no score for them.
            Send("OnRank", local != nil
                ? [NSString stringWithFormat:@"%ld:%ld", (long)local.rank, (long)local.score]
                : @"0:0");
        }];
    }];
}

static NSDictionary *Row(GKLeaderboardEntry *entry)
{
    return @{
        @"rank": @(entry.rank),
        @"name": entry.player.displayName ?: @"",
        @"score": @(entry.score),
        @"isPlayer": @([entry.player.gamePlayerID isEqualToString:GKLocalPlayer.localPlayer.gamePlayerID]),
    };
}

// The top `count` all-time entries and the player's own entry, as JSON to OnTop
// ({"ok":false} when Game Center cannot load the board).
void TreadShredGameCenterLoadTop(const char *board, int count)
{
    NSString *boardId = [NSString stringWithUTF8String:board];
    [GKLeaderboard loadLeaderboardsWithIDs:@[boardId] completionHandler:^(NSArray<GKLeaderboard *> *boards, NSError *error) {
        GKLeaderboard *leaderboard = boards.firstObject;
        if (leaderboard == nil)
        {
            NSLog(@"[TreadShred] Game Center leaderboard not loaded: %@", error.localizedDescription);
            Send("OnTop", @"{\"ok\":false}");
            return;
        }
        [leaderboard loadEntriesForPlayerScope:GKLeaderboardPlayerScopeGlobal timeScope:GKLeaderboardTimeScopeAllTime
                                         range:NSMakeRange(1, (NSUInteger)count)
                             completionHandler:^(GKLeaderboardEntry *local, NSArray<GKLeaderboardEntry *> *entries,
                                                 NSInteger total, NSError *loadError) {
            if (loadError != nil)
            {
                NSLog(@"[TreadShred] Game Center entries not loaded: %@", loadError.localizedDescription);
                Send("OnTop", @"{\"ok\":false}");
                return;
            }
            NSMutableArray *rows = [NSMutableArray array];
            for (GKLeaderboardEntry *entry in entries)
                [rows addObject:Row(entry)];
            NSMutableDictionary *page = [@{@"ok": @YES, @"total": @(total), @"entries": rows} mutableCopy];
            if (local != nil)
                page[@"player"] = Row(local);
            NSData *json = [NSJSONSerialization dataWithJSONObject:page options:0 error:nil];
            Send("OnTop", [[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding]);
        }];
    }];
}
