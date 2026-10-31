using System.Collections.Generic;
using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class BackStackTests
    {
        [Test(Description = "D-014-AC-03: the system Back closes only the panel opened last, then the one beneath it, and does nothing when none is open")]
        public void BackClosesTheTopPanelOnly()
        {
            var closed = new List<string>();
            var stack = new BackStack();
            object settings = new object(), prompt = new object();
            stack.Opened(settings, () => closed.Add("settings"));
            stack.Opened(prompt, () => closed.Add("prompt"));

            Assert.IsTrue(stack.Back());
            CollectionAssert.AreEqual(new[] { "prompt" }, closed);
            Assert.IsTrue(stack.Back());
            CollectionAssert.AreEqual(new[] { "prompt", "settings" }, closed);
            Assert.IsFalse(stack.Back(), "on the start screen Back does nothing");
        }

        [Test(Description = "D-014-AC-03: a panel closed by its own button is no longer closed again by Back")]
        public void PanelsClosedByTheirButtonsLeaveTheStack()
        {
            var closed = 0;
            var stack = new BackStack();
            var panel = new object();
            stack.Opened(panel, () => closed++);
            stack.Closed(panel);
            Assert.IsFalse(stack.Back());
            Assert.AreEqual(0, closed);
        }
    }
}
