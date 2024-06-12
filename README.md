# South African Voting Grafana Demo Project 🇿🇦
This project provides a basic API that mimics the South African voting system. It is used to demonstrate how to utilize the Grafana Labs ecosystem.
🗒️ Logs are sent to Loki (along with Promtail)
📈 Metrics are sent to Prometheus
🔎 Traces are sent to Tempo
🖥️ Dashboards are created in Grafana
✅ Testing is done with k6

## Getting Started
### Prerequisites
- Docker
- Docker Compose

### Running the project
#### Just the API
The API can be run as a standalone service without all the Grafana Labs components. To run the API, simply run it with Visual Studio, selecting the Voting.Grafana project and running the http launch profile.

#### Single Instance
In the single instance setup, the API is scaled to 1 instance. This is done to demonstrate how to use Loki and Prometheus to aggregate logs from a single instance.

To run the single instance setup, run the following command:
```bash
docker-compose up --build
```

To generate data, you'll need to run the following command within the `k6` container:
```bash
k6 run /scripts/vote-test-01.js
```

#### Multi-Instance
In the multi-instance setup, the API is scaled to 2 instances. This is done to demonstrate how to use Loki and Prometheus to aggregate logs from multiple instances.

To run the multi-instance setup, run the following command:
```bash
docker-compose -f docker-compose.yml -f docker-compose.multi-instance.yml up --build
```

To generate data, you'll need to run the following command within the `k6` container:
```bash
k6 run /scripts/vote-test-multi.js
```





