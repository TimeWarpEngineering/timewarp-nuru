// The CLI's generated TimeWarp.Mediator registers handlers only from mediator graph members.
// Without this, dispatching GreetQuery from the CLI throws NoHandlerException.
[assembly: TimeWarp.Mediator.MediatorAssembly]
