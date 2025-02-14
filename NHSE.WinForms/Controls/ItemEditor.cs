﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NHSE.Core;
using NHSE.Sprites;

namespace NHSE.WinForms
{
    public partial class ItemEditor : UserControl
    {
        private readonly List<ComboItem> Recipes = GameInfo.Strings.CreateItemDataSource(RecipeList.Recipes, false);
        private readonly List<ComboItem> Fossils = GameInfo.Strings.CreateItemDataSource(GameLists.Fossils, false);
        private readonly CheckBox[] Watered;

        private bool Loading = true;
        private bool CanExtend;

        public ItemEditor()
        {
            InitializeComponent();

            CB_WrapColor.Items.AddRange(Enum.GetNames(typeof(ItemWrappingPaper)));
            CB_WrapType.Items.AddRange(Enum.GetNames(typeof(ItemWrapping)));

            Watered = new[]
            {
                CHK_WV0, CHK_WV1,
                CHK_WV2, CHK_WV3,
                CHK_WV4, CHK_WV5,
                CHK_WV6, CHK_WV7,
                CHK_WV8, CHK_WV9,
            };
        }

        private IReadOnlyList<ComboItem> AllItems = Array.Empty<ComboItem>();

        public void Initialize(IReadOnlyList<ComboItem> items, bool canExtend = false)
        {
            CHK_IsExtension.Visible = CanExtend = canExtend;

            CB_ItemID.DisplayMember = nameof(ComboItem.Text);
            CB_ItemID.ValueMember = nameof(ComboItem.Value);
            CB_ItemID.DataSource = items;

            CB_Recipe.DisplayMember = nameof(ComboItem.Text);
            CB_Recipe.ValueMember = nameof(ComboItem.Value);
            CB_Recipe.DataSource = Recipes;

            CB_Fossil.DisplayMember = nameof(ComboItem.Text);
            CB_Fossil.ValueMember = nameof(ComboItem.Value);
            CB_Fossil.DataSource = Fossils;

            CB_ItemFilter.DisplayMember = nameof(ComboItem.Text);
            CB_ItemFilter.ValueMember = nameof(ComboItem.Value);

            AllItems = items;

            var test = items.Where(x => x.Value == 02596);

            List<ComboItem> itemKinds = new List<ComboItem>
            {
                new ("None", -1)
            };
            itemKinds.AddRange(from kind in (ItemKind[])Enum.GetValues(typeof(ItemKind)) select new ComboItem(kind.ToString().Replace("Kind_", string.Empty), (int)kind));

            CB_ItemFilter.DataSource = itemKinds;

            LoadItem(Item.NO_ITEM);
        }

        public Item LoadItem(Item item)
        {
            Loading = true;
            var id = item.ItemId;

            if (CB_ItemID.Items.IndexOf((int)id) == -1)
            {
                CB_ItemFilter.SelectedIndex = 0;
            }

            if (CanExtend && id == Item.EXTENSION)
                return LoadExtensionItem(item);

            CHK_IsExtension.Checked = false;
            CB_ItemID.SelectedValue = (int)id;
            var kind = ItemInfo.GetItemKind(id);

            if (kind.IsFlowerGene(id))
            {
                LoadGenes(item.Genes);
                CHK_Gold.Checked = item.IsWateredGold;
                CHK_IsWatered.Checked = item.IsWatered;
                NUD_WaterDays.Value = item.DaysWatered;
                for (int i = 0; i < Watered.Length; i++)
                    Watered[i].Checked = item.GetIsWateredByVisitor(i);
            }
            else
            {
                NUD_Count.Value = item.Count;
                NUD_Uses.Value = item.UseCount;
                NUD_Flag0.Value = item.SystemParam;
            }

            LoadItemTypeValues(kind, id);
            if (kind == ItemKind.Kind_MessageBottle || id >= 60_000)
            {
                NUD_Flag1.Value = item.AdditionalParam;
            }
            else
            {
                CHK_Wrapped.Checked = item.WrappingType != 0;
                CB_WrapType.SelectedIndex = (int)item.WrappingType;
                CB_WrapColor.SelectedIndex = (int)item.WrappingPaper;
                CHK_WrapShowName.Checked = item.WrappingShowItem;
                CHK_Wrap80.Checked = item.Wrapping80;
            }

            Loading = false;
            return item;
        }

        private Item LoadExtensionItem(Item item)
        {
            CB_ItemID.SelectedValue = (int) item.ExtensionItemId;
            CHK_IsExtension.Checked = true;
            NUD_ExtensionX.Value = item.ExtensionX;
            NUD_ExtensionY.Value = item.ExtensionY;
            return item;
        }

        public Item SetItem(Item item)
        {
            if (CHK_IsExtension.Checked)
                return SetExtensionItem(item);

            var id = (ushort)WinFormsUtil.GetIndex(CB_ItemID);
            var kind = ItemInfo.GetItemKind(id);

            item.ItemId = id;
            if (kind.IsFlowerGene(id))
            {
                item.Genes = SaveGenes();
                item.DaysWatered = (int) NUD_WaterDays.Value;
                item.IsWateredGold = CHK_Gold.Checked;
                item.IsWatered = CHK_IsWatered.Checked;
                for (int i = 0; i < Watered.Length; i++)
                    item.SetIsWateredByVisitor(i, Watered[i].Checked);

                item.SystemParam = 0;
                item.AdditionalParam = 0;
            }
            else
            {
                item.Count = (ushort)NUD_Count.Value;
                item.UseCount = (ushort)NUD_Uses.Value;
                item.SystemParam = (byte)NUD_Flag0.Value;
            }

            if (kind == ItemKind.Kind_MessageBottle || id >= 60_000)
            {
                item.AdditionalParam = (byte)NUD_Flag1.Value;
            }
            else
            {
                if (!CHK_Wrapped.Checked)
                {
                    item.SetWrapping(0, 0);
                }
                else
                {
                    var type = (ItemWrapping)CB_WrapType.SelectedIndex;
                    var color = (ItemWrappingPaper)CB_WrapColor.SelectedIndex;
                    var show = CHK_WrapShowName.Checked;
                    var flag = CHK_Wrap80.Checked;
                    item.SetWrapping(type, color, show, flag);
                }
            }
            return item;
        }

        private Item SetExtensionItem(Item item)
        {
            var id = (ushort)WinFormsUtil.GetIndex(CB_ItemID);
            item.ItemId = Item.EXTENSION;
            item.ExtensionItemId = id;
            item.ExtensionX = (byte) NUD_ExtensionX.Value;
            item.ExtensionY = (byte) NUD_ExtensionY.Value;
            return item;
        }

        private void CB_ItemID_SelectedValueChanged(object sender, EventArgs e)
        {
            var itemID = (ushort)WinFormsUtil.GetIndex(CB_ItemID);
            var itemCount = (ushort)NUD_Count.Value;
            ChangeItem(itemID, itemCount);
            var kind = ItemInfo.GetItemKind(itemID);

            ToggleEditorVisibility(kind, itemID);
            if (!Loading)
                LoadItemTypeValues(kind, itemID);

            var remake = ItemRemakeUtil.GetRemakeIndex(itemID);
            if (remake < 0)
            {
                var closeItems = GameInfo.Strings.GetAssociatedItems(itemID, out var bse);
                if (closeItems.Count > 1) // ignore if we are the only parenthesised item
                {
                    L_RemakeBody.Text = $"{bse.Trim()}:\n" + closeItems.ToStringList(false);
                    L_RemakeBody.Visible = true;
                }
                else
                {
                    L_RemakeBody.Visible = false;
                    L_RemakeFabric.Visible = false;
                }
            }
            else
            {
                var info = ItemRemakeInfoData.List[remake];
                var body = info.GetBodySummary(GameInfo.Strings);
                L_RemakeBody.Text = body;
                L_RemakeBody.Visible = body.Length != 0;

                var fabric = info.GetFabricSummary(GameInfo.Strings);
                L_RemakeFabric.Text = fabric;
                L_RemakeFabric.Visible = fabric.Length != 0;
            }
        }

        private void LoadItemTypeValues(ItemKind k, ushort index)
        {
            if (k == ItemKind.Kind_MessageBottle || index >= 60_000)
            {
                CHK_Wrapped.Checked = false;
                CHK_Wrapped.Visible = CHK_Wrapped.Checked = false;
                FLP_Flag1.Visible = true;
                return;
            }

            switch (k)
            {
                case ItemKind.Kind_FossilUnknown:
                    CB_Fossil.SelectedValue = (int) NUD_Count.Value;
                    break;

                case ItemKind.Kind_DIYRecipe:
                    CB_Recipe.SelectedValue = (int) NUD_Count.Value;
                    break;

                case ItemKind.Kind_MessageBottle:
                    CB_Recipe.SelectedValue = (int) NUD_Count.Value;
                    CHK_Wrapped.Visible = CHK_Wrapped.Checked = false;
                    FLP_Flag1.Visible = true;
                    return;
            }

            CHK_Wrapped.Visible  = true;
            FLP_Flag1.Visible = false;
        }

        private void ToggleEditorVisibility(ItemKind k, ushort id)
        {
            if (k.IsFlowerGene(id))
            {
                CB_Recipe.Visible = false;
                FLP_Uses.Visible = FLP_Count.Visible = false;
                FLP_Flower.Visible = true;
                return;
            }

            switch (k)
            {
                case ItemKind.Kind_FossilUnknown:
                    CB_Fossil.Visible = true;

                    CB_Recipe.Visible = false;
                    FLP_Uses.Visible = FLP_Count.Visible = false;
                    FLP_Flower.Visible = false;
                    break;

                case ItemKind.Kind_DIYRecipe:
                    CB_Recipe.Visible = true;

                    CB_Fossil.Visible = false;
                    FLP_Uses.Visible = FLP_Count.Visible = false;
                    FLP_Flower.Visible = false;
                    break;

                case ItemKind.Kind_MessageBottle:
                    CB_Recipe.Visible = true;

                    CB_Fossil.Visible = false;
                    FLP_Uses.Visible = true;
                    FLP_Count.Visible = false;
                    FLP_Flower.Visible = false;
                    break;

                default:
                    CB_Fossil.Visible = false;
                    CB_Recipe.Visible = false;
                    FLP_Uses.Visible = FLP_Count.Visible = true;
                    FLP_Flower.Visible = false;
                    break;
            }
        }

        private void L_Count_DoubleClick(object sender, EventArgs e)
        {
            Item currentItem = SetItem(new Item());
            var result = ItemInfo.TryGetMaxStackCount(currentItem, out var max);
            if (!result)
                return;
            currentItem.Count = (ushort)(max - 1);
            LoadItem(currentItem);
        }

        private void CB_CountAlias_SelectedValueChanged(object sender,EventArgs e)
        {
            var val = WinFormsUtil.GetIndex((ComboBox)sender);
            NUD_Count.Value = Math.Max(0, Math.Min(NUD_Count.Maximum, val));
        }

        private void LoadGenes(FlowerGene genes)
        {
            CHK_R1.Checked = (genes & FlowerGene.R1) != 0;
            CHK_R2.Checked = (genes & FlowerGene.R2) != 0;
            CHK_Y1.Checked = (genes & FlowerGene.Y1) != 0;
            CHK_Y2.Checked = (genes & FlowerGene.Y2) != 0;
            CHK_W1.Checked = (genes & FlowerGene.w1) == 0; // inverted; both bits on = no gene (not white)
            CHK_W2.Checked = (genes & FlowerGene.w2) == 0; // inverted; both bits on = no gene (not white)
            CHK_S1.Checked = (genes & FlowerGene.S1) != 0;
            CHK_S2.Checked = (genes & FlowerGene.S2) != 0;
        }

        private FlowerGene SaveGenes()
        {
            var val = FlowerGene.None;
            if (CHK_R1.Checked) val |= FlowerGene.R1;
            if (CHK_R2.Checked) val |= FlowerGene.R2;
            if (CHK_Y1.Checked) val |= FlowerGene.Y1;
            if (CHK_Y2.Checked) val |= FlowerGene.Y2;
            if (!CHK_W1.Checked) val |= FlowerGene.w1; // inverted; both bits on = no gene (not white)
            if (!CHK_W2.Checked) val |= FlowerGene.w2; // inverted; both bits on = no gene (not white)
            if (CHK_S1.Checked) val |= FlowerGene.S1;
            if (CHK_S2.Checked) val |= FlowerGene.S2;
            return val;
        }

        private void L_WaterDays_Click(object sender, EventArgs e)
        {
            bool value = (ModifierKeys & Keys.Alt) == 0;
            CHK_Gold.Checked = value;
            CHK_IsWatered.Checked = value;
            NUD_WaterDays.Value = value ? 31 : 0;
            foreach (var v in Watered)
                v.Checked = value;
        }

        private void CB_KeyDown(object sender, KeyEventArgs e) => WinFormsUtil.RemoveDropCB(sender, e);

        private void CHK_IsExtension_CheckedChanged(object sender, EventArgs e)
        {
            if (CHK_IsExtension.Checked)
            {
                FLP_Item.Visible = false;
                FLP_Extension.Visible = true;
            }
            else
            {
                FLP_Item.Visible = true;
                FLP_Extension.Visible = false;
            }
        }

        private void ChangeItem(ushort item, ushort count)
        {
            var pb = PB_Item;
            pb.BackColor = ItemColor.GetItemColor(item);
            pb.BackgroundImage = ItemSprite.GetItemSprite(item, count);
        }

        private void CHK_Wrapped_CheckedChanged(object sender, EventArgs e)
        {
            FLP_Wrapped.Visible = CHK_Wrapped.Checked;
            if (CHK_Wrapped.Checked && CB_WrapType.SelectedIndex == 0)
                CB_WrapType.SelectedIndex = (int)ItemWrapping.WrappingPaper;
        }

        private void CB_WrapType_SelectedIndexChanged(object sender, EventArgs e) => CB_WrapColor.Visible = (ItemWrapping)CB_WrapType.SelectedIndex == ItemWrapping.WrappingPaper;

        private void CB_ItemID_TextChanged(object sender, EventArgs e)
        {
            var entered = CB_ItemID.Text;
            var itemNames = AllItems.Where(z => z.Text.Contains(entered)).Take(10).Select(z => z.Text);
            var caption = string.Join(Environment.NewLine, itemNames);
            TT_Search.SetToolTip(CB_ItemID, caption);
        }

        private void NUD_Count_ValueChanged(object sender, EventArgs e)
        {
            var itemID = (ushort)WinFormsUtil.GetIndex(CB_ItemID);
            var itemCount = (ushort)NUD_Count.Value;
            ChangeItem(itemID, itemCount);
        }

        private void PB_Item_Click(object sender, EventArgs e)
        {
            // Import if requested
            if (ModifierKeys == Keys.Shift && Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out var val))
                    return;
                var import = BitConverter.GetBytes(val).ToClass<Item>();
                LoadItem(import);
                System.Media.SystemSounds.Asterisk.Play();
                return;
            }

            // Otherwise, export
            var item = SetItem(new Item());
            var data = item.ToBytesClass();
            var u64 = BitConverter.ToUInt64(data, 0);
            Clipboard.SetText($"{u64:X16}");
            System.Media.SystemSounds.Asterisk.Play();
        }

        private void CB_ItemFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (WinFormsUtil.GetIndex(CB_ItemFilter) != -1)
            {
                var filteredItems = AllItems.Where(x => (int)ItemInfo.GetItemKind((ushort)x.Value) == WinFormsUtil.GetIndex(CB_ItemFilter) || x.Value is Item.EXTENSION or Item.NONE).ToList();
                CB_ItemID.DataSource = filteredItems;
            }
            else
            {
                CB_ItemID.DataSource = AllItems;
            }
        }
    }
}
