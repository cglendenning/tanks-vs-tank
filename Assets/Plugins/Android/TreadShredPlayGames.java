package com.tgts.treadshred;

import android.app.Activity;
import android.util.Log;
import com.google.android.gms.games.AnnotatedData;
import com.google.android.gms.games.AuthenticationResult;
import com.google.android.gms.games.LeaderboardsClient;
import com.google.android.gms.games.Player;
import com.google.android.gms.games.PlayGames;
import com.google.android.gms.games.PlayGamesSdk;
import com.google.android.gms.games.SnapshotsClient;
import com.google.android.gms.games.leaderboard.LeaderboardScore;
import com.google.android.gms.games.leaderboard.LeaderboardVariant;
import com.google.android.gms.games.snapshot.Snapshot;
import com.google.android.gms.games.snapshot.SnapshotMetadataChange;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/**
 * D-025, D-032: Google Play Games for Tread Shred: sign-in, the HIGH SCORE leaderboard and the saved game.
 * Answers reach Unity as the same messages the iOS Game Center and iCloud plugins send, so the shared
 * C# logic (LeaderboardSync, CloudSync) runs unchanged. All Play Games calls run on the UI thread.
 */
public final class TreadShredPlayGames {
    private static final String TAG = "TreadShredPlayGames";
    private static final String LEADERBOARD_RECEIVER = "TreadShredGameCenter";
    private static final String CLOUD_RECEIVER = "TreadShredCloudSave";
    private static final String SAVE_NAME = "tread-shred-progress";

    private static Activity activity;
    private static volatile boolean signedIn;
    // Saved game writes run one at a time; the newest waiting save always lands last.
    private static boolean writing;
    private static String waitingSave;

    private TreadShredPlayGames() {}

    private interface ScoreAnswer {
        void answer(LeaderboardScore score);
    }

    /** Starts the SDK once; Play Games then signs the player in by itself when it can. */
    public static void start(final Activity unityActivity) {
        if (activity != null)
            return;
        activity = unityActivity;
        activity.runOnUiThread(() -> {
            PlayGamesSdk.initialize(activity.getApplicationContext());
            PlayGames.getGamesSignInClient(activity).isAuthenticated()
                .addOnCompleteListener(TreadShredPlayGames::onSignInResult);
        });
    }

    public static boolean isSignedIn() {
        return signedIn;
    }

    /** D-032-AC-03: Play Games' own sign-in, shown only when a signed-out player taps LEADERBOARD. */
    public static boolean offerSignIn() {
        if (activity == null)
            return false;
        activity.runOnUiThread(() -> PlayGames.getGamesSignInClient(activity).signIn()
            .addOnCompleteListener(TreadShredPlayGames::onSignInResult));
        return true;
    }

    private static void onSignInResult(Task<AuthenticationResult> task) {
        signedIn = task.isSuccessful() && task.getResult().isAuthenticated();
        send(LEADERBOARD_RECEIVER, "OnSignIn", signedIn ? "1" : "0");
        if (signedIn)
            loadSave();
    }

    public static void submit(final long score, final String board) {
        if (!ready()) {
            send(LEADERBOARD_RECEIVER, "OnSubmitted", "0");
            return;
        }
        activity.runOnUiThread(() -> PlayGames.getLeaderboardsClient(activity).submitScoreImmediate(board, score)
            .addOnCompleteListener(task -> send(LEADERBOARD_RECEIVER, "OnSubmitted", task.isSuccessful() ? "1" : "0")));
    }

    /** Answers "rank:score" for the player's all-time entry, "0:0" when there is none. */
    public static void loadRank(final String board) {
        if (!ready()) {
            send(LEADERBOARD_RECEIVER, "OnRank", "0:0");
            return;
        }
        activity.runOnUiThread(() -> playerScore(board, score ->
            send(LEADERBOARD_RECEIVER, "OnRank", score == null ? "0:0" : score.getRank() + ":" + score.getRawScore())));
    }

    /** Answers the top entries and the player's own as JSON: {ok, entries[{rank,name,score,isPlayer}], player}. */
    public static void loadTop(final String board, final int count) {
        if (!ready()) {
            send(LEADERBOARD_RECEIVER, "OnTop", "{\"ok\":false}");
            return;
        }
        activity.runOnUiThread(() -> playerScore(board, player -> PlayGames.getLeaderboardsClient(activity)
            .loadTopScores(board, LeaderboardVariant.TIME_SPAN_ALL_TIME, LeaderboardVariant.COLLECTION_PUBLIC, count)
            .addOnCompleteListener(task -> send(LEADERBOARD_RECEIVER, "OnTop", page(task, player)))));
    }

    private static void playerScore(String board, ScoreAnswer answer) {
        PlayGames.getLeaderboardsClient(activity)
            .loadCurrentPlayerLeaderboardScore(board, LeaderboardVariant.TIME_SPAN_ALL_TIME, LeaderboardVariant.COLLECTION_PUBLIC)
            .addOnCompleteListener(task -> answer.answer(
                task.isSuccessful() && task.getResult() != null ? task.getResult().get() : null));
    }

    private static String page(Task<AnnotatedData<LeaderboardsClient.LeaderboardScores>> task, LeaderboardScore player) {
        try {
            JSONObject page = new JSONObject();
            JSONArray entries = new JSONArray();
            LeaderboardsClient.LeaderboardScores scores =
                task.isSuccessful() && task.getResult() != null ? task.getResult().get() : null;
            page.put("ok", scores != null);
            if (scores != null) {
                try {
                    for (LeaderboardScore score : scores.getScores())
                        entries.put(row(score, samePlayer(score, player)));
                } finally {
                    scores.release();
                }
            }
            page.put("entries", entries);
            if (player != null)
                page.put("player", row(player, true));
            return page.toString();
        } catch (JSONException error) {
            Log.e(TAG, "Leaderboard page could not be written", error);
            return "{\"ok\":false}";
        }
    }

    // By player ID: a private profile's own entry has no rank (-1), so ranks cannot identify the player's row.
    private static boolean samePlayer(LeaderboardScore score, LeaderboardScore player) {
        if (player == null)
            return false;
        Player holder = score.getScoreHolder();
        Player self = player.getScoreHolder();
        if (holder != null && self != null)
            return holder.getPlayerId().equals(self.getPlayerId());
        return player.getRank() > 0 && score.getRank() == player.getRank();
    }

    private static JSONObject row(LeaderboardScore score, boolean isPlayer) throws JSONException {
        return new JSONObject().put("rank", score.getRank()).put("name", score.getScoreHolderDisplayName())
            .put("score", score.getRawScore()).put("isPlayer", isPlayer);
    }

    // D-025-AC-03: Play Games keeps the most recently modified version when two devices wrote the saved game.
    private static Task<SnapshotsClient.DataOrConflict<Snapshot>> openSave() {
        return PlayGames.getSnapshotsClient(activity)
            .open(SAVE_NAME, true, SnapshotsClient.RESOLUTION_POLICY_MOST_RECENTLY_MODIFIED);
    }

    /** Reads the saved game after sign-in and answers its JSON ("" when the player has none yet). */
    private static void loadSave() {
        openSave().addOnCompleteListener(task -> {
            if (!task.isSuccessful() || task.getResult().isConflict()) {
                Log.w(TAG, "Saved game unavailable; playing on the local save", task.getException());
                return;
            }
            Snapshot snapshot = task.getResult().getData();
            try {
                String json = new String(snapshot.getSnapshotContents().readFully(), StandardCharsets.UTF_8);
                send(CLOUD_RECEIVER, "OnCloudLoaded", json);
            } catch (IOException error) {
                // Unreadable is not "none": say nothing, so a local save can never replace it.
                Log.e(TAG, "Saved game unreadable", error);
            } finally {
                PlayGames.getSnapshotsClient(activity).discardAndClose(snapshot);
            }
        });
    }

    /** D-025-AC-01: stores the progress JSON as the player's saved game. */
    public static void writeSave(final String json) {
        if (activity == null)
            return;
        activity.runOnUiThread(() -> {
            waitingSave = json;
            if (!writing)
                writeNext();
        });
    }

    private static void writeNext() {
        final String json = waitingSave;
        waitingSave = null;
        writing = json != null && signedIn;
        if (!writing)
            return;
        openSave().addOnCompleteListener(task -> {
            if (!task.isSuccessful() || task.getResult().isConflict()) {
                Log.w(TAG, "Saved game not written", task.getException());
                writeNext();
                return;
            }
            Snapshot snapshot = task.getResult().getData();
            snapshot.getSnapshotContents().writeBytes(json.getBytes(StandardCharsets.UTF_8));
            PlayGames.getSnapshotsClient(activity)
                .commitAndClose(snapshot, new SnapshotMetadataChange.Builder().setDescription("Tread Shred progress").build())
                .addOnCompleteListener(commit -> {
                    if (!commit.isSuccessful())
                        Log.w(TAG, "Saved game not committed", commit.getException());
                    writeNext();
                });
        });
    }

    private static boolean ready() {
        return activity != null && signedIn;
    }

    private static void send(String receiver, String method, String message) {
        UnityPlayer.UnitySendMessage(receiver, method, message);
    }
}
