namespace TeleCore.FlowCore;

/// <summary>
/// The resulting state of a <see cref="ValueContainerBase{TValue}"/> value-change operation.
/// </summary>
public enum ValueState
{
    Incomplete,
    Completed,
    CompletedWithExcess,
    CompletedWithShortage,
    Failed
}