#!/usr/bin/env python3
"""App Store Connect setup for Tread Shred through the App Store Connect API.

    ASC_ISSUER_ID=<issuer> ASC_KEY_ID=<key> python3 scripts/app-store-connect.py game-center
    ... app-store-connect.py icloud
    ... app-store-connect.py adhoc-profile <certificate serial> <device UDID>
    ... app-store-connect.py appstore-profile <certificate serial>
    ... app-store-connect.py listing <version>
    ... app-store-connect.py media <version> <iphone-shots-dir> <ipad-shots-dir> <iphone-preview.mp4>
    ... app-store-connect.py attach-build <version> <build-number>
listing: finds or creates that iOS version (it stays invisible until submitted),
writes the approved English copy from Store/listing-en-US.md (D-030) and removes
every other language from that version and its app info.

icloud: enables the iCloud capability on the App ID, for cloud save (D-025).
adhoc-profile: creates an Ad Hoc profile for the App ID with that distribution
certificate and device, installs it in ~/Library/MobileDevice/Provisioning
Profiles and prints its UUID. Capabilities enabled since the last profile
(iCloud) are only signed into profiles created afterwards.

game-center: enables Game Center for the app, creates the HIGH SCORE
leaderboard with its score limits (D-032) and archives retired boards. Safe to
rerun: existing items are left as they are. The private key is read from
~/.appstoreconnect/private_keys/AuthKey_<ASC_KEY_ID>.p8; only statuses and IDs
are printed, never the key or the signed token.
"""
import base64
import hashlib
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


def bundle_id(api):
    found = [b for b in api.call("GET", f"/v1/bundleIds?filter[identifier]={BUNDLE_ID}")["data"]
             if b["attributes"]["identifier"] == BUNDLE_ID]
    if not found:
        raise SystemExit(f"No App ID {BUNDLE_ID}")
    return found[0]["id"]


def icloud(api):
    app_id = bundle_id(api)
    capabilities = api.call("GET", f"/v1/bundleIds/{app_id}/bundleIdCapabilities")["data"]
    if any(c["attributes"]["capabilityType"] == "ICLOUD" for c in capabilities):
        print("iCloud already enabled on", BUNDLE_ID)
        return
    api.create("bundleIdCapabilities", {
        "capabilityType": "ICLOUD",
        "settings": [{"key": "ICLOUD_VERSION", "options": [{"key": "XCODE_6"}]}],
    }, {"bundleId": {"data": {"type": "bundleIds", "id": app_id}}})
    print("iCloud enabled on", BUNDLE_ID)


def create_profile(api, kind, certificate_serial, udid=None):
    """Creates an Ad Hoc (with that device) or App Store profile for the App ID and that distribution
    certificate, installs it in ~/Library/MobileDevice/Provisioning Profiles and prints its UUID."""
    app_id = bundle_id(api)
    certificates = [c["id"] for c in api.call("GET", "/v1/certificates?filter[certificateType]=DISTRIBUTION")["data"]
                    if c["attributes"]["serialNumber"].upper() == certificate_serial.upper()]
    devices = [d["id"] for d in api.call("GET", f"/v1/devices?filter[udid]={udid}")["data"]] if udid else []
    if not certificates or (udid and not devices):
        raise SystemExit(f"Certificate {certificate_serial} or device {udid} not found")
    label, profile_type = ("Ad Hoc", "IOS_APP_ADHOC") if udid else ("App Store", "IOS_APP_STORE")
    name = f"Tread Shred {label} " + time.strftime("%Y-%m-%d %H%M")
    relationships = {"bundleId": {"data": {"type": "bundleIds", "id": app_id}},
                     "certificates": {"data": [{"type": "certificates", "id": c} for c in certificates]}}
    if devices:
        relationships["devices"] = {"data": [{"type": "devices", "id": d} for d in devices]}
    profile = api.create("profiles", {"name": name, "profileType": profile_type}, relationships)
    uuid = profile["attributes"]["uuid"]
    folder = Path.home() / "Library/MobileDevice/Provisioning Profiles"
    folder.mkdir(parents=True, exist_ok=True)
    (folder / f"{uuid}.mobileprovision").write_bytes(base64.b64decode(profile["attributes"]["profileContent"]))
    print(f"{label} profile created and installed:", name, uuid)


def listing_copy():
    """The approved App Store copy from Store/listing-en-US.md; the (iOS) markers are for Google Play."""
    text = (Path(__file__).resolve().parent.parent / "Store/listing-en-US.md").read_text()
    field = lambda label: text.split(f"**{label}:** ", 1)[1].split("\n", 1)[0].strip()
    description = text.split("## Description\n", 1)[1].strip().replace(" (iOS)", "")
    return {"subtitle": field("App Store subtitle (30)"), "promotionalText": field("App Store promotional text (170)"),
            "keywords": field("App Store keywords (100)"), "description": description}


def listing(api, version):
    copy = listing_copy()
    app = api.call("GET", f"/v1/apps?filter[bundleId]={BUNDLE_ID}")["data"][0]["id"]
    versions = api.call("GET", f"/v1/apps/{app}/appStoreVersions?filter[platform]=IOS&limit=10")["data"]
    editable = [v for v in versions if v["attributes"]["appStoreState"] in ("PREPARE_FOR_SUBMISSION", "DEVELOPER_REJECTED", "REJECTED")]
    if editable:
        target = editable[0]
        if target["attributes"]["versionString"] != version:
            api.call("PATCH", f"/v1/appStoreVersions/{target['id']}", {"data": {"type": "appStoreVersions", "id": target["id"],
                                                                              "attributes": {"versionString": version}}})
    else:
        target = api.create("appStoreVersions", {"platform": "IOS", "versionString": version},
                            {"app": {"data": {"type": "apps", "id": app}}})
    print(f"version {version}: {target['attributes'].get('appStoreState', 'PREPARE_FOR_SUBMISSION')} (not visible until submitted)")
    removed = 0
    for loc in api.call("GET", f"/v1/appStoreVersions/{target['id']}/appStoreVersionLocalizations?limit=50")["data"]:
        if loc["attributes"]["locale"] == "en-US":
            api.call("PATCH", f"/v1/appStoreVersionLocalizations/{loc['id']}", {"data": {"type": "appStoreVersionLocalizations", "id": loc["id"],
                     "attributes": {k: copy[k] for k in ("description", "keywords", "promotionalText")}}})
        else:
            api.call("DELETE", f"/v1/appStoreVersionLocalizations/{loc['id']}")
            removed += 1
    infos = api.call("GET", f"/v1/apps/{app}/appInfos")["data"]
    info = next(i for i in infos if i["attributes"].get("appStoreState", i["attributes"].get("state")) not in ("READY_FOR_SALE", "READY_FOR_DISTRIBUTION"))
    for loc in api.call("GET", f"/v1/appInfos/{info['id']}/appInfoLocalizations?limit=50")["data"]:
        if loc["attributes"]["locale"] == "en-US":
            api.call("PATCH", f"/v1/appInfoLocalizations/{loc['id']}", {"data": {"type": "appInfoLocalizations", "id": loc["id"],
                     "attributes": {"name": "Tread Shred", "subtitle": copy["subtitle"]}}})
        else:
            api.call("DELETE", f"/v1/appInfoLocalizations/{loc['id']}")
            removed += 1
    print(f"en-US copy written; {removed} other-language entries removed")


def en_us_localization(api, version):
    app = api.call("GET", f"/v1/apps?filter[bundleId]={BUNDLE_ID}")["data"][0]["id"]
    target = next(v for v in api.call("GET", f"/v1/apps/{app}/appStoreVersions?filter[platform]=IOS&limit=10")["data"]
                  if v["attributes"]["versionString"] == version)
    loc = next(l for l in api.call("GET", f"/v1/appStoreVersions/{target['id']}/appStoreVersionLocalizations")["data"]
               if l["attributes"]["locale"] == "en-US")
    return app, target, loc


def upload_asset(api, resource, set_type, set_id, path):
    """Reserves an asset, uploads its parts to Apple's upload operations and commits it with its MD5."""
    data = Path(path).read_bytes()
    reserved = api.create(resource, {"fileName": Path(path).name, "fileSize": len(data)},
                          {set_type[:-1]: {"data": {"type": set_type, "id": set_id}}})
    for op in reserved["attributes"]["uploadOperations"]:
        chunk = data[op["offset"]:op["offset"] + op["length"]]
        request = urllib.request.Request(op["url"], data=chunk, method=op["method"])
        for header in op.get("requestHeaders", []):
            request.add_header(header["name"], header["value"])
        urllib.request.urlopen(request, timeout=120).read()
    api.call("PATCH", f"/v1/{resource}/{reserved['id']}", {"data": {"type": resource, "id": reserved["id"],
             "attributes": {"uploaded": True, "sourceFileChecksum": hashlib.md5(data).hexdigest()}}})


def media(api, version, iphone_dir, ipad_dir, preview):
    _, _, loc = en_us_localization(api, version)
    sets = {s["attributes"]["screenshotDisplayType"]: s["id"]
            for s in api.call("GET", f"/v1/appStoreVersionLocalizations/{loc['id']}/appScreenshotSets")["data"]}
    for display, folder in (("APP_IPHONE_67", iphone_dir), ("APP_IPAD_PRO_3GEN_129", ipad_dir), ("APP_IPAD_PRO_129", ipad_dir)):
        set_id = sets.get(display) or api.create("appScreenshotSets", {"screenshotDisplayType": display},
                                                 {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": loc["id"]}}})["id"]
        for old in api.call("GET", f"/v1/appScreenshotSets/{set_id}/appScreenshots")["data"]:
            api.call("DELETE", f"/v1/appScreenshots/{old['id']}")
        shots = sorted(Path(folder).glob("*.png"))
        for shot in shots:
            upload_asset(api, "appScreenshots", "appScreenshotSets", set_id, shot)
        print(f"{display}: {len(shots)} screenshots uploaded")
    previews = {s["attributes"]["previewType"]: s["id"]
                for s in api.call("GET", f"/v1/appStoreVersionLocalizations/{loc['id']}/appPreviewSets")["data"]}
    for preview_type, set_id in previews.items():
        for old in api.call("GET", f"/v1/appPreviewSets/{set_id}/appPreviews")["data"]:
            api.call("DELETE", f"/v1/appPreviews/{old['id']}")
            print(f"{preview_type}: removed {old['attributes'].get('fileName')}")
    set_id = previews.get("IPHONE_67") or api.create("appPreviewSets", {"previewType": "IPHONE_67"},
                                                     {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": loc["id"]}}})["id"]
    upload_asset(api, "appPreviews", "appPreviewSets", set_id, preview)
    print("IPHONE_67 preview uploaded:", Path(preview).name)


def attach_build(api, version, build_number):
    app, target, _ = en_us_localization(api, version)
    for attempt in range(60):
        builds = api.call("GET", f"/v1/builds?filter[app]={app}&filter[version]={build_number}"
                                 f"&filter[preReleaseVersion.version]={version}")["data"]
        state = builds[0]["attributes"]["processingState"] if builds else "NOT YET VISIBLE"
        print(f"build {version} ({build_number}): {state}", flush=True)
        if state == "VALID":
            api.call("PATCH", f"/v1/appStoreVersions/{target['id']}/relationships/build",
                     {"data": {"type": "builds", "id": builds[0]["id"]}})
            print("attached to", version)
            return
        if state in ("FAILED", "INVALID"):
            raise SystemExit(f"Apple rejected the build: {state}")
        time.sleep(30)
    raise SystemExit("Build still processing after 30 minutes")


def main():
    command = sys.argv[1:2]
    issuer, key_id = os.environ.get("ASC_ISSUER_ID"), os.environ.get("ASC_KEY_ID")
    if not issuer or not key_id:
        raise SystemExit("Set ASC_ISSUER_ID and ASC_KEY_ID (see the usage at the top of this script).")
    api = AppStoreConnect(issuer, key_id)
    if command == ["game-center"] and len(sys.argv) == 2:
        game_center(api)
    elif command == ["icloud"] and len(sys.argv) == 2:
        icloud(api)
    elif command == ["adhoc-profile"] and len(sys.argv) == 4:
        create_profile(api, "adhoc", sys.argv[2], sys.argv[3])
    elif command == ["appstore-profile"] and len(sys.argv) == 3:
        create_profile(api, "appstore", sys.argv[2])
    elif command == ["listing"] and len(sys.argv) == 3:
        listing(api, sys.argv[2])
    elif command == ["media"] and len(sys.argv) == 6:
        media(api, *sys.argv[2:6])
    elif command == ["attach-build"] and len(sys.argv) == 4:
        attach_build(api, sys.argv[2], sys.argv[3])
    else:
        raise SystemExit(__doc__)


if __name__ == "__main__":
    main()
