using Godot;
namespace Rehab.Agents.Crate;

public partial class TNT : AgentCrate
{
    public override int BreakAnimSlot => 4;
    public const float CountdownStep = 1.0f;
    bool countingDown;

    public override void Crate_AfterForceBreak()
    {
        DelayedExplosion();
    }

    public override bool OnLandedOn(AgentCharacter agent)
    {
        if (IsBroken) return false;
        if (!countingDown)
        {
            Countdown();
        }
        return true;
    }

    async void Countdown()
    {
        countingDown = true;
        int[] countdownSlots = [8, 9, 10];
        foreach (int slot in countdownSlots)
        {
            if (IsBroken) return;
            DoAnimation(slot, false);
            DoSound(0, 1f, 0f);
            await ToSignal(GetTree().CreateTimer(CountdownStep), SceneTreeTimer.SignalName.Timeout);
        }
        Crate_ForceBreak();
    }
}
