using NUnit.Framework;

public class PlayerGroundContactShadowTests
{
    [Test]
    public void ContactAlpha_IsSolidNearTheGroundAndGoneAboveFadeHeight()
    {
        Assert.That(PlayerGroundContactShadow.CalculateContactAlpha(0.1f, 0.28f, 0.62f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(PlayerGroundContactShadow.CalculateContactAlpha(0.7f, 0.28f, 0.62f), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void ContactAlpha_FadesSmoothlyBetweenThresholds()
    {
        float alpha = PlayerGroundContactShadow.CalculateContactAlpha(0.45f, 0.28f, 0.62f);

        Assert.That(alpha, Is.EqualTo(0.5f).Within(0.0001f));
    }
}
