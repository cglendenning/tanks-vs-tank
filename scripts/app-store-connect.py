#!/usr/bin/env python3
"""App Store Connect setup for Tread Shred through the App Store Connect API.

    ASC_ISSUER_ID=<issuer> ASC_KEY_ID=<key> python3 scripts/app-store-connect.py game-center

game-center: enables Game Center for the app, creates the HIGH SCORE
leaderboard with its score limits (D-032) and archives retired boards. Safe to
rerun: existing items are left as they are. The private key is read from
~/.appstoreconnect/private_keys/AuthKey_<ASC_KEY_ID>.p8; only statuses and IDs
are printed, never the key or the signed token.
"""
import json
import os
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

import jwt

BUNDLE_ID = "com.tgts.tanksvstank"
LEADERBOARD = {
    "vendorIdentifier": "treadshred.best_run",  # TreadShredLeaderboard.GameCenterId
    "referenceName": "High Score",
    "defaultFormatter": "INTEGER",
    "submissionType": "BEST_SCORE",
    "scoreSortType": "DESC",
    "scoreRangeStart": "0",
    "scoreRangeEnd": "250800",  # ScoreRules.MaxRunScore: (218 kills x 100 + 40 clears x 500) x (1 + 2 + 3)
}
LEADERBOARD_NAME = {"locale": "en-US", "name": "HIGH SCORE"}
# Boards the game no longer submits to; archived (reversible), never deleted.
RETIRED = ["treadshred.total_score", "treadshred.high_score"]


class AppStoreConnect:
    def __init__(self, issuer, key_id):
        self.issuer = issuer
        self.key_id = key_id
        self.key = (Path.home() / ".appstoreconnect/private_keys" / f"AuthKey_{key_id}.p8").read_text()

    def call(self, method, path, body=None):
        now = int(time.time())
        token = jwt.encode({"iss": self.issuer, "iat": now, "exp": now + 600, "aud": "appstoreconnect-v1"},
                           self.key, algorithm="ES256", headers={"kid": self.key_id, "typ": "JWT"})
        request = urllib.request.Request("https://api.appstoreconnect.apple.com" + path, method=method,
                                         data=json.dumps(body).encode() if body is not None else None)
        request.add_header("Authorization", "Bearer " + token)
        request.add_header("Content-Type", "application/json")
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                raw = response.read()
                return json.loads(raw) if raw else {}
        except urllib.error.HTTPError as error:
            details = json.loads(error.read() or b"{}").get("errors", [])
            reasons = "; ".join(f"{e.get('code')}: {e.get('detail') or e.get('title')}" for e in details)
            raise SystemExit(f"{method} {path} failed with HTTP {error.code}: {reasons}")

    def create(self, resource_type, attributes, relationships):
        body = {"data": {"type": resource_type, "relationships": relationships}}
        if attributes:
            body["data"]["attributes"] = attributes
        return self.call("POST", "/v1/" + resource_type, body)["data"]


def game_center(api):
    apps = api.call("GET", f"/v1/apps?filter[bundleId]={BUNDLE_ID}")["data"]
    if not apps:
        raise SystemExit(f"No App Store Connect app has bundle ID {BUNDLE_ID}")
    app_id = apps[0]["id"]

    detail = api.call("GET", f"/v1/apps/{app_id}/gameCenterDetail").get("data")
    if detail is None:
        detail = api.create("gameCenterDetails", None, {"app": {"data": {"type": "apps", "id": app_id}}})
        print("Game Center enabled:", detail["id"])
    else:
        print("Game Center already enabled:", detail["id"])

    boards = api.call("GET", f"/v1/gameCenterDetails/{detail['id']}/gameCenterLeaderboards")["data"]
    # Retire old boards first: archived (reversible), and renamed so their names can be reused.
    for retired in boards:
        attributes = retired["attributes"]
        if attributes["vendorIdentifier"] not in RETIRED:
            continue
        name = attributes["referenceName"]
        update = {"archived": True}
        if not name.endswith("(retired)"):
            update["referenceName"] = name + " (retired)"
        if not attributes.get("archived") or "referenceName" in update:
            api.call("PATCH", f"/v1/gameCenterLeaderboards/{retired['id']}", {"data": {
                "type": "gameCenterLeaderboards", "id": retired["id"], "attributes": update}})
            print("Retired leaderboard archived:", attributes["vendorIdentifier"])
    board = next((b for b in boards if b["attributes"]["vendorIdentifier"] == LEADERBOARD["vendorIdentifier"]), None)
    if board is None:
        board = api.create("gameCenterLeaderboards", LEADERBOARD,
                           {"gameCenterDetail": {"data": {"type": "gameCenterDetails", "id": detail["id"]}}})
        print("Leaderboard created:", board["id"])
    else:
        print("Leaderboard already exists:", board["id"])

    names = api.call("GET", f"/v1/gameCenterLeaderboards/{board['id']}/localizations")["data"]
    if not any(n["attributes"]["locale"] == LEADERBOARD_NAME["locale"] for n in names):
        name = api.create("gameCenterLeaderboardLocalizations", LEADERBOARD_NAME,
                          {"gameCenterLeaderboard": {"data": {"type": "gameCenterLeaderboards", "id": board["id"]}}})
        print("Leaderboard name added:", name["id"])

    live = api.call("GET", f"/v1/gameCenterLeaderboards/{board['id']}")["data"]["attributes"]
    drift = {k: (live.get(k), v) for k, v in LEADERBOARD.items() if live.get(k) != v}
    if drift:
        raise SystemExit(f"Leaderboard settings differ from this script (live, expected): {drift}")
    print("Leaderboard settings verified:", LEADERBOARD["vendorIdentifier"],
          f"scores {LEADERBOARD['scoreRangeStart']}-{LEADERBOARD['scoreRangeEnd']}")


def main():
    if sys.argv[1:] != ["game-center"]:
        raise SystemExit(__doc__)
    issuer, key_id = os.environ.get("ASC_ISSUER_ID"), os.environ.get("ASC_KEY_ID")
    if not issuer or not key_id:
        raise SystemExit("Set ASC_ISSUER_ID and ASC_KEY_ID (see the usage at the top of this script).")
    game_center(AppStoreConnect(issuer, key_id))


if __name__ == "__main__":
    main()
