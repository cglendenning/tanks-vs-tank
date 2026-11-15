using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // A player gets two free Starter Pack test drives in all; then the purchase stands alone (D-037-AC-08).
    public sealed class StarterPackTestDriveLimitTests
    {
        // Round-trips through JsonUtility, as PlayerPrefs and the cloud snapshot do, so a reload sees only saved fields.
        private sealed class JsonWalletStore : IWalletStore
        {
            private string saved;
            public WalletState Load() => saved == null ? null : JsonUtility.FromJson<WalletState>(saved);
            public void Save(WalletState state) => saved = JsonUtility.ToJson(state);
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-037-AC-08: a player gets two Starter Pack test drives in all, counted as each starts, saved and restored with the wallet; a reset gives them back")]
        public void TwoTestDrivesInAll()
        {
            Assert.AreEqual(2, StarterPackPitch.TestDrives);
            Assert.AreEqual((true, true, false, false),
                (StarterPackPitch.CanTestDrive(0), StarterPackPitch.CanTestDrive(1), StarterPackPitch.CanTestDrive(2), StarterPackPitch.CanTestDrive(3)));

            var store = new JsonWalletStore();
            var wallet = new GemWallet(store);
            Assert.AreEqual(0, wallet.StarterPackTestDrives);
            wallet.CountStarterPackTestDrive();
            wallet.CountStarterPackTestDrive();
            Assert.AreEqual(2, new GemWallet(store).StarterPackTestDrives, "saved with the wallet");
            var restored = new GemWallet(new JsonWalletStore());
            restored.Replace(JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(new WalletState { starterPackTestDrives = 2 })));
            Assert.AreEqual(2, restored.StarterPackTestDrives, "restored from a cloud save (D-025)");
            wallet.Replace(new WalletState());
            Assert.AreEqual(0, wallet.StarterPackTestDrives, "a reset (D-044) gives them back");

            var offer = Script("Game/TreadShredStarterOffer.cs");
            StringAssert.Contains("var canDrive = StarterPackPitch.CanTestDrive(TreadShredStore.Ensure().Wallet.StarterPackTestDrives);", offer);
            StringAssert.Contains("offer.Build(root, font, offer.limit, overMission && canDrive);", offer);
            Assert.Less(offer.IndexOf("TreadShredStore.Ensure().Wallet.CountStarterPackTestDrive();", System.StringComparison.Ordinal),
                offer.IndexOf("TreadShredTestDrive.Begin(driveFont, StarterPackPitch.Stack", System.StringComparison.Ordinal), "counted as the drive starts");
        }
    }
}
