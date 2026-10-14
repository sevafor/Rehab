using Godot;
namespace Rehab.Agents.Crate;

public partial class IronSpring : AgentCrate
{
    public override bool IsBreakable => false;
    public override float BounceScale => 1.5f;

    public override bool OnLandedOn(AgentCharacter agent)
    {
        PlaySpringSquash(2);
        return true;
    }
}
