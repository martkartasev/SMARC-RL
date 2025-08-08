namespace Network.Message
{
    public struct Observation
    {
        public float[] Continuous;
        public int[] Discrete;
    }

    public struct Parameters
    {
        public float[] Continuous;
    }

    public struct Action
    {
        public float[] Continuous;
        public int[] Discrete;
    }
}