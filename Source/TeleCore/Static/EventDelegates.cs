namespace TeleCore.Static;

public delegate void ThingSpawnedEvent(ThingStateChangedEventArgs args);
public delegate void ThingDespawnedEvent(ThingStateChangedEventArgs args);
public delegate void ThingStateChangedEvent(ThingStateChangedEventArgs args);

//Hediffs
public delegate void PawnHediffChangedEvent(PawnHediffChangedEventArgs args);