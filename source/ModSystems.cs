using Vintagestory.API.Common;

namespace Tabards;

public sealed class Tabards : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterCollectibleBehaviorClass("Tabards:Token", typeof(TokenBehavior));
    }
}