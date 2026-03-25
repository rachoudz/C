# Billing App Project Plan

## Stack
- C# WinForms
- SQLite
- 3 layers: Domain / Application / Infrastructure

## MVP Scope
- Customers
- Products/services
- Invoices
- Invoice items
- Storage-days pricing for one special item
- VAT calculation
- Save/edit draft invoices
- Print/PDF later

## Build Order
1. Database schema
2. Domain models
3. Calculation service
4. Invoice form UI
5. Save/load invoices
6. Invoice list/search
7. Print/PDF

## Main Rule
Storage charge = BillableDays × DailyRate × Quantity

## Day Count (default suggestion)
Inclusive calendar days with minimum 1 day.
This can be changed after business confirmation.
