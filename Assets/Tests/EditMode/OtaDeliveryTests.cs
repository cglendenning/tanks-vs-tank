using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class OtaDeliveryTests
    {
        [Test(Description = "D-015-AC-01: the OTA helper prints no install link until the install page, manifest and IPA all answer HTTP 200")]
        public void NoLinkBeforeEveryUrlAnswers()
        {
            var script = File.ReadAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "scripts/serve-tank-ota.sh"));
            var check = script.IndexOf("for URL in \"${TUNNEL_URL}/install.html\" \"${TUNNEL_URL}/manifest.xml\" \"${TUNNEL_URL}/TreadShred.ipa\"");
            Assert.Greater(check, 0, "all three URLs are checked");
            StringAssert.Contains("curl --resolve \"$TUNNEL_HOST:443:$TUNNEL_IP\" --http1.1 -fsSI", script, "-f fails on any status but 2xx");
            Assert.Less(check, script.IndexOf("echo \"OTA_URL="), "the check runs before any link is printed");
            Assert.Less(script.IndexOf("cat > \"$OTA_DIR/install.html\""), check, "the install page exists before it is checked");
        }
    }
}
