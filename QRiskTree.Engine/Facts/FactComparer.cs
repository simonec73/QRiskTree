
using QRiskTree.Engine.Facts;

namespace QriskTree.Engine.Facts
{
    class FactComparer : IEqualityComparer<Fact>
    {
        public bool Equals(Fact? x, Fact? y)
        {
            if (x == null || y == null)
            {
                return (x == null && y == null);                
            }
            else
            {
                if (x.Id == y.Id) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id.Equals(y.Id);
            }
        }

        public int GetHashCode(Fact obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
