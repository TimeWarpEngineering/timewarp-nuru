// Temporary test to verify invoker registration
#pragma warning disable IDE0008, RCS1163

using TimeWarp.Nuru;

Console.WriteLine($"SyncCount before: {InvokerRegistry.SyncCount}");
Console.WriteLine($"AsyncCount before: {InvokerRegistry.AsyncCount}");

// Try to find String_Int
var method = ((Action<string, int>)((s, i) => { })).Method;
var signatureKey = InvokerRegistry.ComputeSignatureKey(method);
Console.WriteLine($"Computed signature key: '{signatureKey}'");

bool found = InvokerRegistry.TryGetSync(signatureKey, out var invoker);
Console.WriteLine($"Found invoker: {found}");

if (!found)
{
    Console.WriteLine("Invoker NOT found - falling back to DynamicInvoke");
}
else
{
    Console.WriteLine("Invoker found - would use generated code");
}

return 0;
