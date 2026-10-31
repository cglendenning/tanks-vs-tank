#!/usr/bin/env python3
"""Google Play Console setup for Tread Shred through the Android Publisher API.

    python3 scripts/play-console.py products                  # create or update the gem packs (D-007-AC-02) and activate them
    python3 scripts/play-console.py check-upload-key <aab>    # upload into a throwaway edit: does Google accept its signing key?

Authenticates with the service account key at $PLAY_KEY_FILE (default
~/.play-console/tread-shred-play.json); the account needs Tread Shred permissions in the Play Console.
"""
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

import jwt

PACKAGE = "com.tgts.tankkvstank"
API = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/" + PACKAGE
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


def call(method, path, auth, body=None, query=None):
    url = API + path + ("?" + urllib.parse.urlencode(query) if query else "")
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


if __name__ == "__main__":
    if len(sys.argv) == 2 and sys.argv[1] == "products":
        products(token())
    elif len(sys.argv) == 3 and sys.argv[1] == "check-upload-key":
        check_upload_key(token(), sys.argv[2])
    else:
        sys.exit(__doc__)
