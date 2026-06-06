namespace OrderProcessor;

// Pure domain logic: which transitions are legal. No I/O — the highest-value
// unit-test target (lesson 8). The order lifecycle:
//
//   PLACED -> CONFIRMED -> PREPARING -> DISPATCHED -> DELIVERED
//      └──────────┴──────────┴── CANCELLED   (CANCEL allowed only before DISPATCH)
//
public static class OrderStateMachine
{
    // Returns the resulting state for a LEGAL transition, or null if illegal.
    public static string? Next(string currentState, string commandType)
    {
        // TODO(you) 6.1 — implement the transition table. Map (currentState, commandType):
        //     ("PLACED",     "CONFIRM")  => "CONFIRMED"
        //     ("CONFIRMED",  "PREPARE")  => "PREPARING"
        //     ("PREPARING",  "DISPATCH") => "DISPATCHED"
        //     ("DISPATCHED", "DELIVER")  => "DELIVERED"
        //     ("PLACED" or "CONFIRMED" or "PREPARING", "CANCEL") => "CANCELLED"
        //     anything else => null   (illegal: e.g. DELIVER before DISPATCH, CANCEL after DISPATCH)
        //
        // hint: a switch expression reads cleanly —
        //   return (currentState, commandType) switch
        //   {
        //       ("PLACED", "CONFIRM") => "CONFIRMED",
        //       ...
        //       ("PLACED" or "CONFIRMED" or "PREPARING", "CANCEL") => "CANCELLED",
        //       _ => null,
        //   };
        return null;
    }
}
