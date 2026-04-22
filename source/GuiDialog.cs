using AttributeRenderingLibrary;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Tabards;

public class GuiDialogHeraldryWorkbench : GuiDialogBlockEntity
{
    private int selectedIndex = 0;
    private int prevSlotOver = -1;

    protected override double FloatyDialogPosition => 0.75;

    private string[] SlotIcons => ["heraldry_dye", "heraldry_flag"];

    public GuiDialogHeraldryWorkbench(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi, int selectedIndex) : base(dialogTitle, inventory, blockEntityPos, capi)
    {
        if (IsDuplicate) return;

        this.selectedIndex = selectedIndex;

        capi.World.Player.InventoryManager.OpenInventory(Inventory);

        SetupDialog();
    }

    private void OnInventorySlotModified(int slotid)
    {
        capi.Event.EnqueueMainThreadTask(SetupDialog, "setupHeraldryWorkbenchDlg");
    }

    void SetupDialog()
    {
        if (!capi.Gui.Icons.CustomIcons.Any(x => SlotIcons.Contains(x.Key)))
        {
            registerIcons();
        }

        List<SkillItem> patterns = LoadPatterns();
        prevSlotOver = -1;
        ClearComposers();

        ElementBounds stationBounds = ElementBounds.Fixed(0, 0, 200, 90);

        ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30, 1, 1);
        ElementBounds dyeSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 90, 30, 1, 1);
        ElementBounds previewSlotBounds = ElementBounds.Fixed(0, 100, 140, 140);
        ElementBounds craftButtonBounds = ElementBounds.Fixed(0, 250, 140, 50);
        ElementBounds selectedPatternTextBounds = ElementBounds.Fixed(180, 30, 500, 25);
        ElementBounds patternGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 180, 70, 1, 1);

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(stationBounds);

        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

        SingleComposer = capi.Gui.CreateCompo("blockentity_heraldry_heraldryworkbench" + BlockEntityPosition, dialogBounds);
        SingleComposer.AddShadedDialogBG(bgBounds);
        SingleComposer.AddDialogTitleBar(DialogTitle, OnTitleBarClose);
        SingleComposer.BeginChildElements(bgBounds);

        SingleComposer.AddItemSlotGrid(Inventory, SendInvPacket, 1, [0], inputSlotBounds, "inputSlot");
        SingleComposer.AddIf(Inventory[0].Empty);
        SingleComposer.AddHoverText(Lang.Get("heraldry:slothelp-input"), CairoFont.WhiteSmallishText(), 200, inputSlotBounds.FlatCopy(), "inputSlotTooltip");
        SingleComposer.EndIf();

        SingleComposer.AddItemSlotGrid(Inventory, SendInvPacket, 1, [2], dyeSlotBounds, "dyeSlot");
        SingleComposer.AddIf(Inventory[2].Empty);
        SingleComposer.AddHoverText(Lang.Get("heraldry:slothelp-dye"), CairoFont.WhiteSmallishText(), 200, dyeSlotBounds.FlatCopy(), "dyeSlotTooltip");
        SingleComposer.EndIf();

        SingleComposer.AddIf(GenerateOutputStack(Inventory, selectedIndex, consumeLiquid: false) != null);
        SingleComposer.AddButton(Lang.Get("heraldry:craft"), SendCraftingPacket, craftButtonBounds, key: "craftButton");
        SingleComposer.EndIf();

        SingleComposer.AddInset(previewSlotBounds.FlatCopy());
        SingleComposer.AddRichtext(GenerateOutputStackPreview(), previewSlotBounds, "previewSlot");

        SingleComposer.AddSkillItemGrid(patterns, 20, 10, OnSelectedPattern, patternGridBounds, "pattern_grid");
        SingleComposer.GetSkillItemGrid("pattern_grid").OnSlotOver += (index) => OnPatternOver(patterns, index);
        SingleComposer.AddDynamicText("", CairoFont.WhiteSmallishText(), selectedPatternTextBounds, "selected_pattern_name");

        SingleComposer.EndChildElements();
        SingleComposer.Compose();

        if (patterns.Count != 0 && patterns.Count > selectedIndex)
        {
            OnSelectedPattern(selectedIndex);
        }
    }

    private void registerIcons()
    {
        capi.Gui.Icons.CustomIcons["heraldry_dye"] = capi.Gui.Icons.SvgIconSource(AssetLocation.Create("heraldry:textures/icons/dye.svg"));
        capi.Gui.Icons.CustomIcons["heraldry_flag"] = capi.Gui.Icons.SvgIconSource(AssetLocation.Create("heraldry:textures/icons/folded-flag.svg"));
    }

    private void OnPatternOver(List<SkillItem> patterns, int index)
    {
        if (index < patterns.Count && index != prevSlotOver)
        {
            prevSlotOver = index;
            SingleComposer.GetDynamicText("selected_pattern_name").SetNewText(patterns[index].Name);
        }
    }

    private void OnSelectedPattern(int index)
    {
        SingleComposer.GetSkillItemGrid("pattern_grid").selectedIndex = index;
        selectedIndex = index;
        SingleComposer.GetRichtext("previewSlot").SetNewText(GenerateOutputStackPreview());

        capi.Network.SendBlockEntityPacket(BlockEntityPosition, (int)EnumHeraldryPacket.SelectDesign, selectedIndex);
    }

    private void SendInvPacket(object packet)
    {
        capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
    }

    private bool SendCraftingPacket()
    {
        capi.Network.SendBlockEntityPacket(BlockEntityPosition, (int)EnumHeraldryPacket.ApplyDesign);
        return true;
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        Inventory.SlotModified += OnInventorySlotModified;
    }

    public override void OnGuiClosed()
    {
        Inventory.SlotModified -= OnInventorySlotModified;

        SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
        SingleComposer.GetSlotGrid("dyeSlot").OnGuiClosed(capi);

        base.OnGuiClosed();
    }

    private RichTextComponentBase[] GenerateOutputStackPreview() =>
    [
        new ItemstackTextComponent(capi, GenerateOutputStack(Inventory, selectedIndex, consumeLiquid: false), 128)
    ];

    public static ItemStack GenerateOutputStack(IInventory inv, int index, bool consumeLiquid)
    {
        ItemSlot inputSlot = inv[0];
        ItemSlot dyeSlot = inv[2];

        if (inputSlot.Empty || dyeSlot.Empty) return null;

        ItemStack inputStack = inputSlot.Itemstack.Clone();
        ItemStack dyeStack = dyeSlot.Itemstack.Clone();

        HeraldryDyeProperties dyeProps = HeraldryDyeProperties.GetPropertiesFrom(dyeStack);
        HeraldryProperties patternProps = HeraldryProperties.GetPropertiesFrom(inputStack);
        ILiquidSource liquidCnt = dyeStack.Collectible.GetCollectibleInterface<ILiquidSource>();

        if (!patternProps.HasEnoughLiquid(liquidCnt, dyeStack))
            return null;

        if (!dyeProps.IsBleach && !patternProps.HasColor(dyeProps.Color))
            return null;

        Variants variants = Variants.FromStack(inputStack);

        if (dyeProps.IsBleach)
        {
            ItemStack bleachedStack = HandleBleach(inputStack, variants);
            if (consumeLiquid && bleachedStack != null)
            {
                ConsumeLiquid(liquidCnt, dyeSlot, patternProps.ConsumeLitres);
            }
            return bleachedStack;
        }

        List<string> availablePatterns = [.. patternProps.AvailablePatterns];
        bool hasOnlyOneLayer = HasOnlyOneLayer(variants);
        if (hasOnlyOneLayer)
            availablePatterns.Insert(0, "color");

        if (index >= availablePatterns.Count)
            return null;

        string nextLayer = (hasOnlyOneLayer && index == 0) ? "layer1" : GetNextLayer(variants);
        if (string.IsNullOrEmpty(nextLayer))
            return null;

        variants.Set(nextLayer, $"{availablePatterns[index]}_{dyeProps.Color}");
        variants.ToStack(inputStack);

        if (consumeLiquid)
        {
            ConsumeLiquid(liquidCnt, dyeSlot, patternProps.ConsumeLitres);
        }
        return inputStack;
    }

    private List<SkillItem> LoadPatterns()
    {
        List<SkillItem> patterns = [];
        ItemSlot inputSlot = Inventory[0];
        ItemSlot dyeSlot = Inventory[2];

        if (inputSlot.Empty || dyeSlot.Empty)
            return patterns;

        HeraldryDyeProperties dyeProps = HeraldryDyeProperties.GetPropertiesFrom(dyeSlot.Itemstack);
        if (dyeProps?.IsBleach == true)
            return patterns;

        HeraldryProperties patternProps = HeraldryProperties.GetPropertiesFrom(inputSlot.Itemstack);
        ILiquidSource liquidCnt = dyeSlot.Itemstack.Collectible.GetCollectibleInterface<ILiquidSource>();

        if (!patternProps.HasEnoughLiquid(liquidCnt, dyeSlot.Itemstack))
            return patterns;

        if (!patternProps.HasColor(dyeProps.Color))
            return patterns;

        Variants inputVariants = Variants.FromStack(inputSlot.Itemstack);

        if (!string.IsNullOrEmpty(inputVariants.Get("layer8")))
            return patterns;

        bool hasOnlyOneLayer = HasOnlyOneLayer(inputVariants);

        if (hasOnlyOneLayer)
            patterns.Add(BuildSkillItem(inputSlot.Itemstack.Clone(), layer: "layer1", fullPattern: $"color_{dyeProps.Color}"));

        foreach (string pattern in patternProps.AvailablePatterns)
            patterns.Add(BuildSkillItem(inputSlot.Itemstack.Clone(), layer: "layer2", fullPattern: $"{pattern}_{dyeProps.Color}"));

        return patterns;
    }

    private SkillItem BuildSkillItem(ItemStack renderStack, string layer, string fullPattern)
    {
        Variants variants = Variants.FromStack(renderStack);
        RemoveAllLayersExcept(variants, "layer1", layer);
        variants.Set("layer1", "color_plain");
        variants.Set(layer, $"{fullPattern}");
        variants.ToStack(renderStack);

        return new SkillItem
        {
            Code = $"pattern-{fullPattern}",
            Name = Lang.GetMatching($"heraldry:pattern-{fullPattern}"),
            Description = Lang.GetMatching($"heraldry:pattern-{fullPattern}"),
            RenderHandler = renderStack.RenderItemStack(capi, false, false)
        };
    }

    private static ItemStack HandleBleach(ItemStack inputStack, Variants variants)
    {
        string lastLayer = GetLastLayer(variants);
        bool canBleach = variants.Get("layer1") != "color_plain" || !string.IsNullOrEmpty(variants.Get("layer2"));

        if (string.IsNullOrEmpty(lastLayer) || !canBleach) return null;

        if (lastLayer == "layer1")
            variants.Set("layer1", "color_plain");
        else
            variants.RemoveKeys(lastLayer);

        variants.ToStack(inputStack);
        return inputStack;
    }

    private static void ConsumeLiquid(ILiquidSource liquidCnt, ItemSlot slot, float litres)
    {
        WaterTightContainableProps content = liquidCnt.GetContentProps(slot.Itemstack);
        int quantity = (int)(content.ItemsPerLitre * litres);
        liquidCnt.TryTakeContent(slot.Itemstack, quantity);
    }

    private static bool HasOnlyOneLayer(Variants v)
    {
        return !string.IsNullOrEmpty(v.Get("layer1")) && string.IsNullOrEmpty(v.Get("layer2"));
    }

    private static string GetNextLayer(Variants v) =>
        Enumerable.Range(1, 8).Select(i => $"layer{i}").FirstOrDefault(k => string.IsNullOrEmpty(v.Get(k)));

    private static string GetLastLayer(Variants v) =>
        Enumerable.Range(1, 8).Reverse().Select(i => $"layer{i}").FirstOrDefault(k => !string.IsNullOrEmpty(v.Get(k)));

    private static void RemoveAllLayersExcept(Variants v, params string[] keep)
    {
        for (int i = 7; i >= 0; i--)
        {
            string k = $"layer{i}";
            if (keep.Contains(k)) break;
            v.RemoveKeys(k);
        }
    }
}