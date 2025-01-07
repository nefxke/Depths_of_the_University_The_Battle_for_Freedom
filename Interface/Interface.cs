using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Interface
{
    public class ItemAddedEventArgs : EventArgs
    {
        public IItem Item { get; }
        public int Count { get; }

        public ItemAddedEventArgs(IItem item, int count)
        {
            Item = item;
            Count = count;
        }
    }

    public static class InventoryEvents
    {
        public static event EventHandler<ItemAddedEventArgs> ItemAdded;

        public static void OnItemAdded(IItem item, int count)
        {
            ItemAdded?.Invoke(null, new ItemAddedEventArgs(item, count));
        }
    }

    public interface IPlayerInventory
    {
        bool AddItem(IItem item, int count = 1);
        bool RemoveItem(IItem item);
        int GetItemCount(IItem item);
        Dictionary<IItem, int> GetAllItems();
    }

    public interface IItem
    {
        string Name { get; }
        string Description { get; }
        int Duration { get; }
        int Durability { get; set; }
        BitmapImage Image { get; }
        float HealthBonus { get; }
        float EnergyBonus { get; }
        float EnergyRegenerationBonus { get; }
        float HealthRegenerationBonus { get; }
        byte DamageBonus { get; }
        int IntellectBonus { get; }
        int DexterityBonus { get; }
        int ExpBonus { get; }
    }
    public interface IPlayerUI
    {
        void ShowEnergyMessage(string message);
        void FlashEnergyValue();
    }
    public interface IItemDescription
    {
        void ShowItemDescription(string description);
        void HideItemDescription();
    }
    public interface IUseItem
    {
        void UseItem(string itemName);
    }
}