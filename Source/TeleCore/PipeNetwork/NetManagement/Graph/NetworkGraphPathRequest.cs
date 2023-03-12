using System;

namespace TeleCore;

public struct NetworkGraphPathRequest
{
    private readonly int _depth;
    private readonly INetworkSubPart _requester = null;
    private readonly INetworkSubPart _target;
    private readonly NetworkRole _role;
    private readonly RequestValidator _validator;
    internal readonly Predicate<INetworkSubPart> _dirtyChecker;
    
    public INetworkSubPart Requester => _requester;
    public int Depth => _depth;

    public NetworkGraphPathRequest(TransactionRequest transactionRequest)
    {
        _depth = transactionRequest.maxDepth;
        _requester = transactionRequest.requester;
        _role = transactionRequest.requestedRole;
        _validator = (RequestValidator)transactionRequest.partValidator;
        _dirtyChecker = transactionRequest.dirtyChecker;
     
        //
        _target = null;
    }

    public NetworkGraphPathRequest(INetworkSubPart requester, INetworkSubPart target)
    {
        _depth = int.MaxValue;
        _requester = requester;
        _target = target;
        _role = 0;
        _validator = RequestValidator.Invalid;
        _dirtyChecker = null;
    }

    public struct RequestValidator
    {
        private readonly Predicate<INetworkSubPart> _validator;
        
        public static implicit operator Predicate<INetworkSubPart>(RequestValidator validator) => validator._validator;
        public static explicit operator RequestValidator(Predicate<INetworkSubPart> predicate) => new RequestValidator(predicate);

        public static RequestValidator Invalid => new RequestValidator(null);

        public bool IsValid => _validator != null;
        
        private RequestValidator(Predicate<INetworkSubPart> validator)
        {
            _validator = validator;
        }

        public readonly bool Invoke(INetworkSubPart obj)
        {
            return _validator?.Invoke(obj) ?? false;
        }
        
        public override int GetHashCode()
        {
            return _validator?.Method?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is RequestValidator validator)
            {
                return GetHashCode() == validator.GetHashCode();
            }

            if (obj is Predicate<INetworkSubPart> predicate)
            {
                return GetHashCode() == predicate.Method.GetHashCode();
            }
            return false;
        }
    }
    
    public bool Fits(INetworkSubPart part)
    {
        if (_target != null && part != _target) return false;
        if (_role > 0 && !part.NetworkRole.HasFlag(_role)) return false;
        if (_validator.IsValid && !_validator.Invoke(part)) return false;

        return true;
    }

    public override string ToString()
    {
        return $"Hash: {GetHashCode()}\n" +
               $" |Requester: {_requester}" +
               $" |Role: {_role}" +
               $" |Target: {_target}";
    }
    
    public override bool Equals(object obj)
    {
        if (obj is NetworkGraphPathRequest request)
        {
            return request._requester == _requester &&
                   request._target == _target &&
                   request._role == _role &&
                   request._validator.Equals(_validator);
        }
        return false;
    }

    public bool Equals(NetworkGraphPathRequest other)
    {
        return Equals(_requester, other._requester) && Equals(_target, other._target) && _role == other._role && _validator.Equals(other._validator);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (_requester != null ? _requester.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (_target != null ? _target.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int)_role;
            hashCode = (hashCode * 397) ^ _validator.GetHashCode();
            return hashCode;
        }
    }
    
}
