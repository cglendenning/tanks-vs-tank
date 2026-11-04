using System;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// D-037: every screen test starts with the Starter Pack's window already over, so its interstitial never covers the
    /// screens they check; StarterPackTests reopens it for its own checks. The test runner restores the editor's
    /// PlayerPrefs afterwards.
    /// </summary>
    [SetUpFixture]
    public sealed class StarterPackWindowSetup
    {
        [OneTimeSetUp]
        public void EndTheWindow()
        {
            var storeType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TreadShredStore")).First(t => t != null);
            var store = storeType.GetMethod("Ensure").Invoke(null, null);
            var wallet = (GemWallet)storeType.GetProperty("Wallet").GetValue(store);
            var state = JsonUtility.FromJson<WalletState>(PlayerPrefs.GetString("TreadShredGemWallet", "{}")) ?? new WalletState();
            state.starterPackWindowStartTicks = DateTime.UtcNow.AddHours(-1).Ticks;
            wallet.Replace(state);
        }
    }
}
