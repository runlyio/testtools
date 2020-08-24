# Runly.TestTools.SignalR
Test tools for testing code that uses SignalR.

## How to Use

Use the extension method `ListenFor` on SignalR's `HubConnection` to create an event listener which you can then await.

```csharp

async Task Should_send_hub_message_on_order_cancelled(HubConnection con, StoreFrontApi api)
{
  var events = con.ListenFor("NewOrder", "OrderCancelled");
  
  await con.StartAsync();
  
  var order = await api.PlaceOrderAsync("john@acme.com", new Cart()
  {
    new Item("SKU123", 1),
    new Item("SKU987", 3)
  });
  
  await api.CancelOrderAsync(order.Id);
  
  await events.When("NewOrder").And("OrderCancelled").WithTimeout(10);
}

```
