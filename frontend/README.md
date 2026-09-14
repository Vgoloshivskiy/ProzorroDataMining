Prozorro Dashboard

Development

1. Install dependencies: npm install
2. Run dev server: npm run dev

The app will attempt to call backend endpoints:
- GET /api/dashboard/summary?id=ID (optional)
- POST /api/import (optional) to trigger import

If the endpoints are not available, the dashboard will show mock data.
