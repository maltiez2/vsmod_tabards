using AttributeRenderingLibrary;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        EntityPlayer? player = byEntity as EntityPlayer;
        EntityPlayer? target = player?.EntitySelection?.Entity as EntityPlayer;
        if (player?.Controls.Sneak == true)
        {
            target = player;
        }
        IInventory? inventory = target?.Player?.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
        ItemSlot? tabardSlot = inventory?.FirstOrDefault(slot => WildcardUtil.Match("tabard-*", slot?.Itemstack?.Collectible?.Code?.Path ?? ""), null);

        if (tabardSlot == null)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        Variants tokenVariants = Variants.FromStack(slot.Itemstack);
        Variants tabardVariants = Variants.FromStack(tabardSlot.Itemstack);

        string? emblem = tokenVariants.Get("emblem");
        string? tabardEmblem = tabardVariants.Get("emblem");

        if (emblem == tabardEmblem)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        if (emblem == null)
        {
            tabardVariants.RemoveKeys("emblem");
        }
        else
        {
            tabardVariants.Set("emblem", emblem);
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
