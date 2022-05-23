namespace KahaGameCore.Combat
{
    public class ValueObject 
    {
        public string UID { get; private set; }
        public string Tag { get; private set; }
        public int Value { get; private set; }

        public ValueObject(string tag, int value)
        {
            UID = System.Guid.NewGuid().ToString();
            Tag = tag;
            Value = value;
        }

        public void Add(int add, int max = int.MaxValue, int min = int.MinValue)
        {
            long _longValue = Value + add;
            if (_longValue > max)
            {
                _longValue = max;
            }
            if (_longValue < min)
            {
                _longValue = min;
            }
            Value = (int)_longValue;
        }

        public void Mutiply(float mutiply, int max = int.MaxValue, int min = int.MinValue)
        {
            float _floatValue = (float)Value * mutiply;
            if (_floatValue > max)
            {
                _floatValue = max;
            }
            if (_floatValue < min)
            {
                _floatValue = min;
            }
            Value = (int)_floatValue;
        }

        public void Set(int value)
        {
            Value = value;
        }
    }
}