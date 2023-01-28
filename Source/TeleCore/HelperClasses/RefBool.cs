namespace TeleCore;

public class RefBool
{
    private bool _bool;

    public bool Bool => _bool;
    
    public static implicit operator bool(RefBool refBool) => refBool._bool;
    public static explicit operator RefBool(bool boolean) => new RefBool(boolean);
            
    public RefBool(bool boolean)
    {
        _bool = boolean;
    }

    public void SetBool(bool newVal)
    {
        _bool = newVal;
    }
}