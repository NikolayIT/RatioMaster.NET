namespace RatioMaster_source
{
    using System.Collections;

    internal class RMCollection<item> : CollectionBase
    {
        internal item this[int index]
        {
            get
            {
                return (item)this.List[index];
            }

            set
            {
                this.List[index] = value;
            }
        }

        internal int Add(item value)
        {
            return this.List.Add(value);
        }
    }
}