using NUnit.Framework;
namespace DianaPortfolio.Tests {
 public sealed class CubeJumpTests {
  [Test]
  public void JumpOffset_ReachesExactlyTenUnitsAtApex() {
   Assert.That(CubeJump.JumpOffset(0.5f, 10f), Is.EqualTo(10f).Within(0.001f));
  }
  [Test]
  public void JumpOffset_IsZeroAtStartAndLanding() {
   Assert.That(CubeJump.JumpOffset(0f, 10f), Is.EqualTo(0f).Within(0.001f));
   Assert.That(CubeJump.JumpOffset(1f, 10f), Is.EqualTo(0f).Within(0.001f));
  }
 }
}