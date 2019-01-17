namespace DistributedDojo
{
    public static class Factory
    {
        public static IMessagingEndpoint CreateServiceBus()
        {
            return new InMemoryBus();
        }
    }
}