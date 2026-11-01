#!/usr/bin/env python3
"""Google Play Console setup for Tread Shred through the Android Publisher API.

    python3 scripts/play-console.py products                  # create or update the gem packs (D-007-AC-02) and activate them
    python3 scripts/play-console.py check-upload-key <aab>    # upload into a throwaway edit: does Google accept its signing key?
    python3 scripts/play-console.py release <aab> <fraction>  # one edit: bundle, staged production rollout, listing text and images
    python3 scripts/play-console.py rollout <fraction>        # change the live production rollout; 1 completes it for everyone
    python3 scripts/play-console.py listing                   # write the listing text and promo video alone (no bundle)
    python3 scripts/play-console.py status                    # Google's review state of each production release
    python3 scripts/play-console.py games-leaderboard <id>    # Play Games HIGH SCORE board with score limits (D-032-AC-04)

Authenticates with the service account key at $PLAY_KEY_FILE (default
~/.play-console/tread-shred-play.json); the account needs Tread Shred permissions in the Play Console.
"""
import json
import re
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

import jwt
import store_copy

PACKAGE = "com.tgts.tankkvstank"
API = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/" + PACKAGE
GAMES_API = "https://gamesconfiguration.googleapis.com/games/v1configuration/applications/"
# D-032-AC-04: the Play Games HIGH SCORE board, with the same limits as Game Center's (TreadShredLeaderboard.PlayGamesId).
LEADERBOARD = {
    "token": "treadshred.best_run",
    "scoreOrder": "LARGER_IS_BETTER",
    "scoreMin": "0",
    "scoreMax": "250800",  # ScoreRules.MaxRunScore: (218 kills x 100 + 40 clears x 500) x (1 + 2 + 3)
}
# R-GEM-PACKS: the same product IDs and USD prices as App Store Connect (GemCatalog.Packs).
PACKS = [("treadshred.gems100", 100, "0.99"), ("treadshred.gems550", 550, "4.99"), ("treadshred.gems1200", 1200, "9.99")]
PURCHASE_OPTION = "buy"


def token():
    key = json.load(open(os.path.expanduser(os.environ.get("PLAY_KEY_FILE", "~/.play-console/tread-shred-play.json"))))
    now = int(time.time())
    assertion = jwt.encode({"iss": key["client_email"], "scope": "https://www.googleapis.com/auth/androidpublisher",
                            "aud": key["token_uri"], "iat": now, "exp": now + 600}, key["private_key"], algorithm="RS256")
    body = urllib.parse.urlencode({"grant_type": "urn:ietf:params:oauth:grant-type:jwt-bearer", "assertion": assertion})
    return json.load(urllib.request.urlopen(key["token_uri"], body.encode()))["access_token"]


def call(method, path, auth, body=None, query=None, base=API):
    url = base + path + ("?" + urllib.parse.urlencode(query) if query else "")
    request = urllib.request.Request(url, method=method, data=json.dumps(body).encode() if body is not None else None,
                                     headers={"Authorization": "Bearer " + auth, "Content-Type": "application/json"})
    try:
        return json.loads(urllib.request.urlopen(request).read() or b"{}")
    except urllib.error.HTTPError as error:
        raw = error.read().decode(errors="replace")
        try:
            message = json.loads(raw).get("error", {}).get("message", raw)
        except ValueError:
            message = raw
        sys.exit(f"{method} {path}: HTTP {error.code}: {message[:400]}")


def money(usd):
    units, cents = usd.split(".")
    return {"currencyCode": "USD", "units": units, "nanos": int(cents.ljust(9, "0"))}


def products(auth):
    for product_id, gems, usd in PACKS:
        converted = call("POST", "/pricing:convertRegionPrices", auth, {"price": money(usd)})
        regions = [{"regionCode": code, "price": entry["price"], "availability": "AVAILABLE"}
                   for code, entry in sorted(converted.get("convertedRegionPrices", {}).items())]
        other = converted.get("convertedOtherRegionsPrice", {})
        product = {
            "packageName": PACKAGE,
            "productId": product_id,
            "listings": [{"languageCode": "en-US", "title": f"{gems:,} GEMS",
                          "description": f"{gems:,} GEMS FOR COMBAT BOOSTS AND TANK UPGRADES."}],
            "purchaseOptions": [{
                "purchaseOptionId": PURCHASE_OPTION,
                "buyOption": {"legacyCompatible": True, "multiQuantityEnabled": False},
                "regionalPricingAndAvailabilityConfigs": regions,
                "newRegionsConfig": {"availability": "AVAILABLE", "usdPrice": other.get("usdPrice", money(usd)),
                                     "eurPrice": other.get("eurPrice")},
            }],
        }
        call("PATCH", "/onetimeproducts/" + product_id, auth, product,
             {"allowMissing": "true", "regionsVersion.version": converted["regionVersion"]["version"],
              "updateMask": "listings,purchaseOptions"})
        call("POST", f"/oneTimeProducts/{product_id}/purchaseOptions:batchUpdateStates", auth,
             {"requests": [{"activatePurchaseOptionRequest": {"packageName": PACKAGE, "productId": product_id,
                                                              "purchaseOptionId": PURCHASE_OPTION}}]})
        state = call("GET", "/oneTimeProducts/" + product_id, auth)["purchaseOptions"][0].get("state")
        print(f"{product_id}: {gems:,} gems, US ${usd}, {len(regions)} regions, {state}")


def check_upload_key(auth, bundle):
    """Uploads the bundle into a new edit and deletes the edit: nothing is released or kept."""
    edit = call("POST", "/edits", auth, {})["id"]
    try:
        url = API.replace("/androidpublisher/v3/", "/upload/androidpublisher/v3/") + f"/edits/{edit}/bundles?uploadType=media"
        request = urllib.request.Request(url, method="POST", data=open(bundle, "rb").read(),
                                         headers={"Authorization": "Bearer " + auth, "Content-Type": "application/octet-stream"})
        try:
            uploaded = json.loads(urllib.request.urlopen(request, timeout=600).read())
            print(f"Google accepted the bundle's upload key (version code {uploaded.get('versionCode')}); the edit is discarded.")
        except urllib.error.HTTPError as error:
            raw = error.read().decode(errors="replace")
            try:
                message = json.loads(raw).get("error", {}).get("message", raw)
            except ValueError:
                message = raw
            print(f"Google rejected the bundle: HTTP {error.code}: {message[:400]}")
    finally:
        call("DELETE", f"/edits/{edit}", auth)


ROOT = Path(__file__).resolve().parent.parent
MEDIA = ROOT / "Store/media/1.9.0/google-play"
# The listing's promo video: Store/media/1.9.0/creatives/play-promo-1920x1080.mp4 on Craig's YouTube channel.
PROMO_VIDEO = "https://www.youtube.com/watch?v=PawgvD2AA10"


def play_listing():
    """The approved copy for Google Play (store_copy.py), the (iOS) lines left out, with the promo video."""
    copy = store_copy.copy_for("android")
    return {"title": copy["name"], "shortDescription": copy["shortDescription"], "fullDescription": copy["description"],
            "video": PROMO_VIDEO}


def upload(auth, path, url):
    # Play takes bundles as octet-stream but images only with their own media type.
    kind = "image/png" if str(path).endswith(".png") else "application/octet-stream"
    request = urllib.request.Request(url, method="POST", data=Path(path).read_bytes(),
                                     headers={"Authorization": "Bearer " + auth, "Content-Type": kind})
    try:
        return json.loads(urllib.request.urlopen(request, timeout=600).read() or b"{}")
    except urllib.error.HTTPError as error:
        sys.exit(f"upload {Path(path).name}: HTTP {error.code}: {error.read().decode(errors='replace')[:300]}")


def release(auth, bundle, fraction):
    """One edit: uploads the bundle, releases it to production (fraction 1 is a full release, below 1 a staged
    rollout), writes the listing and replaces its images."""
    upload_api = API.replace("/androidpublisher/v3/", "/upload/androidpublisher/v3/")
    edit = call("POST", "/edits", auth, {})["id"]
    version_code = upload(auth, bundle, f"{upload_api}/edits/{edit}/bundles?uploadType=media")["versionCode"]
    # Google takes a full release as "completed" with no fraction; a fraction must stay below 1.
    rollout = {"status": "completed"} if float(fraction) >= 1 else {"status": "inProgress", "userFraction": float(fraction)}
    call("PUT", f"/edits/{edit}/tracks/production", auth, {"track": "production", "releases": [dict(rollout,
        versionCodes=[str(version_code)],
        releaseNotes=[{"language": "en-US", "text": store_copy.copy_for("android")["releaseNotes"]}])]})
    listing = play_listing()
    call("PATCH", f"/edits/{edit}/listings/en-US", auth, dict(listing, language="en-US"))
    for kind, files in (("phoneScreenshots", sorted((MEDIA / "phone").glob("*.png"))),
                        ("sevenInchScreenshots", sorted((MEDIA / "tablet").glob("*.png"))),
                        ("tenInchScreenshots", sorted((MEDIA / "tablet").glob("*.png"))),
                        ("featureGraphic", [MEDIA / "feature-graphic-1024x500.png"])):
        call("DELETE", f"/edits/{edit}/listings/en-US/{kind}", auth)
        for image in files:
            upload(auth, image, f"{upload_api}/edits/{edit}/listings/en-US/{kind}?uploadType=media")
        print(f"{kind}: {len(files)} uploaded")
    call("POST", f"/edits/{edit}:commit", auth)
    print(f"Released version code {version_code} to production at {min(float(fraction), 1):.0%} with the approved listing.")


def listing(auth):
    """Writes the listing text and promo video in an edit of its own. Google reviews it, together with anything
    already in review."""
    edit = call("POST", "/edits", auth, {})["id"]
    call("PATCH", f"/edits/{edit}/listings/en-US", auth, dict(play_listing(), language="en-US"))
    call("POST", f"/edits/{edit}:commit", auth)
    print(f"Listing written with promo video {PROMO_VIDEO}")


def rollout(auth, fraction):
    """Moves the production release in progress to that fraction of users; 1 completes it for everyone."""
    edit = call("POST", "/edits", auth, {})["id"]
    track = call("GET", f"/edits/{edit}/tracks/production", auth)
    staged = [r for r in track.get("releases", []) if r.get("status") == "inProgress"]
    if not staged:
        call("DELETE", f"/edits/{edit}", auth)
        sys.exit("No production release is in progress")
    release = staged[0]
    if float(fraction) >= 1:
        release["status"] = "completed"
        release.pop("userFraction", None)
    else:
        release["userFraction"] = float(fraction)
    call("PUT", f"/edits/{edit}/tracks/production", auth, {"track": "production", "releases": [release]})
    call("POST", f"/edits/{edit}:commit", auth)
    print(f"Production version code {release['versionCodes'][0]}: {release['status']}"
          + (f" at {float(fraction):.0%}" if release["status"] == "inProgress" else " for 100% of users"))


def status(auth):
    """Prints Google Play's review lifecycle state for each production release (in review, approved, published)."""
    for release in call("GET", "/tracks/production/releases", auth).get("releases", []):
        codes = ", ".join(str(a["versionCode"]) for a in release.get("activeArtifacts", []))
        state = release["releaseLifecycleState"].removeprefix("RELEASE_LIFECYCLE_STATE_")
        print(f"production {release['releaseName']} (version code {codes}): {state}")


def games_leaderboard(auth, application_id):
    """Creates the HIGH SCORE leaderboard in Play Games Services (draft until the game's Play Games
    configuration is published), or finds it, and prints its ID for TreadShredLeaderboard.PlayGamesId."""
    boards = call("GET", f"{application_id}/leaderboards", auth, base=GAMES_API).get("items", [])
    board = next((b for b in boards if b.get("token") == LEADERBOARD["token"]), None)
    if board is None:
        board = call("POST", f"{application_id}/leaderboards", auth, dict(LEADERBOARD, draft={
            "name": {"translations": [{"locale": "en-US", "value": "HIGH SCORE"}]},
            "scoreFormat": {"numberFormatType": "NUMERIC", "numDecimalPlaces": 0},
            "sortRank": 1}), base=GAMES_API)
    print(f"Play Games leaderboard {board['id']}: scores {board.get('scoreMin')} to {board.get('scoreMax')}, {board.get('scoreOrder')}")


if __name__ == "__main__":
    if len(sys.argv) == 2 and sys.argv[1] == "products":
        products(token())
    elif len(sys.argv) == 3 and sys.argv[1] == "check-upload-key":
        check_upload_key(token(), sys.argv[2])
    elif len(sys.argv) == 4 and sys.argv[1] == "release":
        release(token(), sys.argv[2], sys.argv[3])
    elif len(sys.argv) == 3 and sys.argv[1] == "games-leaderboard":
        games_leaderboard(token(), sys.argv[2])
    elif len(sys.argv) == 2 and sys.argv[1] == "listing":
        listing(token())
    elif len(sys.argv) == 2 and sys.argv[1] == "status":
        status(token())
    elif len(sys.argv) == 3 and sys.argv[1] == "rollout":
        rollout(token(), sys.argv[2])
    else:
        sys.exit(__doc__)
