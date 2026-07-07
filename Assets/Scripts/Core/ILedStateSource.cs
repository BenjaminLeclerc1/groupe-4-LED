namespace LedShow.Core
{
    // Anything that can produce a LedState: a fake pattern generator today,
    // the render-to-texture sampler tomorrow. Consumers (simulator, ArtNet
    // router) only ever depend on this, never on a concrete source.
    public interface ILedStateSource
    {
        LedState State { get; }
    }
}
