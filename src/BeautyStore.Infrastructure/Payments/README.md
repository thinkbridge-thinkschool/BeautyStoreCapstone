# Infrastructure / Payments

Concrete adapter for the payment gateway.

## Contents (planned)

| File | Purpose |
|---|---|
| `RazorpayGateway.cs` | Implements `IPaymentGateway` using Razorpay REST API |
| `RazorpayOptions.cs` | Config POCO: ApiKey, ApiSecret, WebhookSecret |
| `WebhookSignatureVerifier.cs` | Validates HMAC signature on incoming Razorpay webhooks |

## Swap note

Because Application only depends on the `IPaymentGateway` interface (defined in Domain/Application), replacing Razorpay with Stripe requires only writing a new `StripeGateway.cs` in this folder and updating the DI registration — no Application or Domain code changes.
