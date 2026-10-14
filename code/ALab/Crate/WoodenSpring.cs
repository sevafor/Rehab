using Godot;
namespace Rehab.Agents.Crate;
public partial class WoodenSpring : AgentCrate
{
    public override float BounceScale => 1.5f;

    public override bool OnLandedOn(AgentCharacter agent)
    {
        if (IsBroken) return false;
        PlaySpringSquash(2);
        return true;
    }
}
