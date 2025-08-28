using AttributeRenderingLibrary;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Tabards;

public class TokenBehavior : CollectibleBehavior
{
    public TokenBehavior(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        AddAllTypesToCreativeInventory(api);
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        _canApplyToOthers = properties["canApplyToOthers"].AsBool(false);
        _targetWildcard = properties["targetWildcard"].AsString("*");
        _skipVariants = properties["skipVariants"].AsObject<string[]>([]);
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        EntityPlayer? target = null;
        EntityPlayer? player = byEntity as EntityPlayer;
        EntityPlayer? selection = player?.EntitySelection?.Entity as EntityPlayer;
        
        if (player?.Controls.Sneak == false &&_canApplyToOthers)
        {
            target = selection;
        }
        else
        {
            target = player;
        }
        
        IInventory? inventory = target?.Player?.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
        ItemSlot? tabardSlot = inventory?.FirstOrDefault(slot => WildcardUtil.Match(_targetWildcard, slot?.Itemstack?.Collectible?.Code?.ToString() ?? ""), null);

        if (tabardSlot == null)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        Variants tokenVariants = Variants.FromStack(slot.Itemstack);
        Variants tabardVariants = Variants.FromStack(tabardSlot.Itemstack);

        Dictionary<string, string>? elements = (Dictionary<string, string>?)_variants_Elements?.GetValue(tokenVariants);

        if (elements == null) return;

        foreach ((string variantCode, _) in elements)
        {
            if (_skipVariants.Contains(variantCode)) continue;

            string? emblem = tokenVariants.Get(variantCode);
            string? tabardEmblem = tabardVariants.Get(variantCode);

            if (emblem == tabardEmblem)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            if (emblem == null)
            {
                tabardVariants.RemoveKeys(variantCode);
            }
            else
            {
                tabardVariants.Set(variantCode, emblem);
            }
        }

        tabardVariants.ToStack(tabardSlot.Itemstack);

        if (slot.Itemstack.Item.GetMaxDurability(slot.Itemstack) > 0)
        {
            slot.Itemstack.Item.DamageItem(player.Api.World, player, slot, 1);
        }

        tabardSlot.MarkDirty();

        handling = EnumHandling.Handled;
        handHandling = EnumHandHandling.Handled;
    }

    private static readonly PropertyInfo? _variants_Elements = typeof(Variants).GetProperty("Elements", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
    private bool _canApplyToOthers = false;
    private string _targetWildcard = "*";
    private string[] _skipVariants = [];

    private void AddAllTypesToCreativeInventory(ICoreAPI api)
    {
        if (api.Side == EnumAppSide.Client) return;

        IEnumerable<string> emblems = api.Assets.GetLocations("textures/emblems", "tabards").Select(asset => asset.Path.Split('/').Last().Split('.').First());

        List<JsonItemStack> stacks = [];

        foreach (string emblem in emblems)
        {
            stacks.Add(GenStackJson(string.Format("{{\"types\": {{\"emblem\": \"{0}\"}}}}", emblem), api));
        }

        JsonItemStack noAttributesStack = new()
        {
            Code = collObj.Code,
            Type = EnumItemClass.Item
        };
        noAttributesStack.Resolve(api.World, "token");

        collObj.CreativeInventoryStacks = [
            new() { Stacks = [noAttributesStack], Tabs =  ["general", "tabards"] },
            new() { Stacks = [.. stacks], Tabs = ["general", "tabards"] }
        ];
        collObj.CreativeInventoryTabs = null;
    }
    private JsonItemStack GenStackJson(string json, ICoreAPI api)
    {
        JsonItemStack stackJson = new()
        {
            Code = collObj.Code,
            Type = EnumItemClass.Item,
            Attributes = new JsonObject(JToken.Parse(json))
        };

        stackJson.Resolve(api.World, "token");

        return stackJson;
    }
}
