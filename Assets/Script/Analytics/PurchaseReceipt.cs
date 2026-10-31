using System;
using System.Text;

namespace TreadShred.Analytics
{
    public readonly struct GooglePlayPurchase
    {
        public GooglePlayPurchase(string purchaseData, string signature)
        {
            PurchaseData = purchaseData;
            Signature = signature;
        }

        public string PurchaseData { get; }
        public string Signature { get; }
    }

    // D-023-AC-02: the Google Play purchase data and signature inside a Unity IAP receipt, which Tenjin needs to
    // verify a purchase before crediting its revenue to a campaign. Unity IAP wraps them as
    // {"Store":"GooglePlay","Payload":"{\"json\":\"...\",\"signature\":\"...\"}"}.
    public static class PurchaseReceipt
    {
        // Null for any receipt that is not a complete Google Play receipt.
        public static GooglePlayPurchase? GooglePlay(string unityReceipt)
        {
            if (JsonText.String(unityReceipt, "Store") != "GooglePlay")
                return null;
            var payload = JsonText.String(unityReceipt, "Payload");
            var data = JsonText.String(payload, "json");
            var signature = JsonText.String(payload, "signature");
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(signature))
                return null;
            return new GooglePlayPurchase(data, signature);
        }
    }

    // Reads one string field of a JSON object. This assembly has no engine or package references, so no JSON
    // library; quotes inside string values are always escaped, so a bare "key": can only be a key.
    internal static class JsonText
    {
        public static string String(string json, string key)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            var marker = "\"" + key + "\"";
            for (var at = json.IndexOf(marker, StringComparison.Ordinal); at >= 0;
                 at = json.IndexOf(marker, at + 1, StringComparison.Ordinal))
            {
                var colon = SkipSpace(json, at + marker.Length);
                if (colon >= json.Length || json[colon] != ':')
                    continue;
                var open = SkipSpace(json, colon + 1);
                return open < json.Length && json[open] == '"' ? ReadString(json, open + 1) : null;
            }
            return null;
        }

        private static int SkipSpace(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            return index;
        }

        // Null when the string is unterminated or carries a malformed escape.
        private static string ReadString(string json, int index)
        {
            var text = new StringBuilder();
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"')
                    return text.ToString();
                if (c != '\\')
                {
                    text.Append(c);
                    continue;
                }
                if (index >= json.Length)
                    return null;
                var escape = json[index++];
                switch (escape)
                {
                    case '"': text.Append('"'); break;
                    case '\\': text.Append('\\'); break;
                    case '/': text.Append('/'); break;
                    case 'b': text.Append('\b'); break;
                    case 'f': text.Append('\f'); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 't': text.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length ||
                            !int.TryParse(json.Substring(index, 4), System.Globalization.NumberStyles.HexNumber, null, out var code))
                            return null;
                        text.Append((char)code);
                        index += 4;
                        break;
                    default:
                        return null;
                }
            }
            return null;
        }
    }
}
