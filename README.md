# AzFunctionWarmup
Extension that adds a AddSingletonWithWarmup method to IServiceCollection. Instances need to implement IWarmup and define a WarmupAsync method. The method is called before azure functions are discovered, but after all dependencies are registered. Supports dependency injection for warmup classes.

## License
MIT License
