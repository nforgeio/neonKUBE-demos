namespace HelloWorldOperator
{
    public static class Dashboard
    {
        public static string DashboardJson = """
{
  "annotations": {
    "list": [
      {
        "builtIn": 1,
        "datasource": {
          "type": "datasource",
          "uid": "grafana"
        },
        "enable": true,
        "hide": true,
        "iconColor": "rgba(0, 211, 255, 1)",
        "name": "Annotations & Alerts",
        "target": {
          "limit": 100,
          "matchAny": false,
          "tags": [],
          "type": "dashboard"
        },
        "type": "dashboard"
      }
    ]
  },
  "editable": true,
  "fiscalYearStartMonth": 0,
  "graphTooltip": 0,
  "id": 9,
  "iteration": 1656643341231,
  "links": [],
  "liveNow": false,
  "panels": [
    {
      "datasource": {},
      "fieldConfig": {
    "defaults": {
        "color": {
            "mode": "palette-classic"
          },
          "custom": {
            "axisLabel": "",
            "axisPlacement": "auto",
            "barAlignment": 0,
            "drawStyle": "line",
            "fillOpacity": 10,
            "gradientMode": "none",
            "hideFrom": {
                "legend": false,
              "tooltip": false,
              "viz": false
            },
            "lineInterpolation": "linear",
            "lineWidth": 1,
            "pointSize": 5,
            "scaleDistribution": {
                "type": "linear"
            },
            "showPoints": "never",
            "spanNulls": true,
            "stacking": {
                "group": "A",
              "mode": "none"
            },
            "thresholdsStyle": {
                "mode": "off"
            }
        },
          "mappings": [],
          "thresholds": {
            "mode": "absolute",
            "steps": [
              {
                "color": "green",
                "value": null
              },
              {
                "color": "red",
                "value": 80
              }
            ]
          },
          "unit": "percentunit"
        },
        "overrides": []
      },
      "gridPos": {
    "h": 8,
        "w": 12,
        "x": 0,
        "y": 0
      },
      "id": 6,
      "interval": "1m",
      "options": {
    "legend": {
        "calcs": [],
          "displayMode": "list",
          "placement": "bottom"
        },
        "tooltip": {
        "mode": "single",
          "sort": "none"
        }
},
      "pluginVersion": "8.3.1",
      "targets": [
        {
          "datasource": {
            "type": "prometheus",
            "uid": "mimir"
          },
          "exemplar": true,
          "expr": "kube_deployment_status_replicas_available{deployment=\"hello-world\"} / kube_deployment_status_replicas{deployment=\"hello-world\"}",
          "interval": "",
          "legendFormat": "Available",
          "refId": "A"
        },
        {
    "datasource": {
        "type": "prometheus",
            "uid": "mimir"
          },
          "exemplar": true,
          "expr": "kube_deployment_status_replicas_unavailable{deployment=\"hello-world\"} / kube_deployment_status_replicas{deployment=\"hello-world\"}",
          "hide": false,
          "interval": "",
          "legendFormat": "Unavailable",
          "refId": "B"
        }
      ],
      "title": "Available Replicas",
      "type": "timeseries"
    },
    {
    "datasource": { },
      "fieldConfig": {
        "defaults": {
            "color": {
                "mode": "continuous-RdYlGr"
            },
          "decimals": 0,
          "mappings": [
            {
                "options": {
                    "match": "null",
                "result": {
                        "text": "N/A"
                }
                },
              "type": "special"
            }
          ],
          "max": 100,
          "min": 0,
          "thresholds": {
                "mode": "absolute",
            "steps": [
              {
                    "color": "green",
                "value": null
              },
              {
                    "color": "red",
                "value": 80
              }
            ]
          },
          "unit": "percent"
        },
        "overrides": []
      },
      "gridPos": {
        "h": 5,
        "w": 12,
        "x": 12,
        "y": 0
      },
      "id": 15,
      "interval": "1m",
      "links": [],
      "maxDataPoints": 100,
      "options": {
        "orientation": "horizontal",
        "reduceOptions": {
            "calcs": [
              "mean"
          ],
          "fields": "",
          "values": false
        },
        "showThresholdLabels": false,
        "showThresholdMarkers": true
      },
      "pluginVersion": "9.0.0",
      "targets": [
        {
        "datasource": {
            "type": "prometheus",
            "uid": "cortex"
          },
          "exemplar": false,
          "expr": "sum(kube_deployment_status_replicas_available{deployment=\"hello-world\"}) / sum(kube_deployment_status_replicas{deployment=\"hello-world\"}) * 100",
          "instant": true,
          "interval": "",
          "intervalFactor": 2,
          "legendFormat": "",
          "refId": "A",
          "step": 40
        }
      ],
      "title": "Replicas",
      "type": "gauge"
    },
    {
    "datasource": {
        "type": "prometheus",
        "uid": "mimir"
      },
      "fieldConfig": {
        "defaults": {
            "color": {
                "mode": "thresholds"
            },
          "decimals": 0,
          "mappings": [
            {
                "options": {
                    "match": "null",
                "result": {
                        "text": "N/A"
                }
                },
              "type": "special"
            }
          ],
          "thresholds": {
                "mode": "absolute",
            "steps": [
              {
                    "color": "green",
                "value": null
              },
              {
                    "color": "red",
                "value": 80
              }
            ]
          },
          "unit": "none"
        },
        "overrides": []
      },
      "gridPos": {
        "h": 3,
        "w": 3,
        "x": 12,
        "y": 5
      },
      "id": 11,
      "interval": "1m",
      "links": [],
      "maxDataPoints": 100,
      "options": {
        "colorMode": "none",
        "graphMode": "none",
        "justifyMode": "auto",
        "orientation": "horizontal",
        "reduceOptions": {
            "calcs": [
              "mean"
          ],
          "fields": "",
          "values": false
        },
        "textMode": "auto"
      },
      "pluginVersion": "9.0.0",
      "targets": [
        {
        "datasource": {
            "type": "prometheus",
            "uid": "mimir"
          },
          "exemplar": false,
          "expr": "sum(kube_deployment_status_replicas_available{deployment=\"hello-world\"})",
          "instant": true,
          "interval": "",
          "intervalFactor": 2,
          "legendFormat": "",
          "refId": "A",
          "step": 40
        }
      ],
      "title": "Available",
      "type": "stat"
    },
    {
    "datasource": { },
      "fieldConfig": {
        "defaults": {
            "color": {
                "mode": "thresholds"
            },
          "decimals": 0,
          "mappings": [
            {
                "options": {
                    "match": "null",
                "result": {
                        "text": "N/A"
                }
                },
              "type": "special"
            }
          ],
          "thresholds": {
                "mode": "absolute",
            "steps": [
              {
                    "color": "green",
                "value": null
              },
              {
                    "color": "red",
                "value": 80
              }
            ]
          },
          "unit": "none"
        },
        "overrides": []
      },
      "gridPos": {
        "h": 3,
        "w": 3,
        "x": 15,
        "y": 5
      },
      "id": 13,
      "interval": "1m",
      "links": [],
      "maxDataPoints": 100,
      "options": {
        "colorMode": "none",
        "graphMode": "none",
        "justifyMode": "auto",
        "orientation": "horizontal",
        "reduceOptions": {
            "calcs": [
              "mean"
          ],
          "fields": "",
          "values": false
        },
        "textMode": "auto"
      },
      "pluginVersion": "9.0.0",
      "targets": [
        {
        "datasource": {
            "type": "prometheus",
            "uid": "cortex"
          },
          "exemplar": false,
          "expr": "sum(kube_deployment_status_replicas_unavailable{deployment=\"hello-world\"})",
          "instant": true,
          "interval": "",
          "intervalFactor": 2,
          "legendFormat": "",
          "refId": "A",
          "step": 40
        }
      ],
      "title": "Unavailable",
      "type": "stat"
    },
    {
    "datasource": {
        "type": "prometheus",
        "uid": "mimir"
      },
      "fieldConfig": {
        "defaults": {
            "color": {
                "mode": "thresholds"
            },
          "mappings": [],
          "thresholds": {
                "mode": "absolute",
            "steps": [
              {
                    "color": "text",
                "value": null
              },
              {
                    "color": "red",
                "value": 80
              }
            ]
          }
        },
        "overrides": []
      },
      "gridPos": {
        "h": 3,
        "w": 3,
        "x": 18,
        "y": 5
      },
      "id": 20,
      "interval": "1m",
      "options": {
        "colorMode": "value",
        "graphMode": "area",
        "justifyMode": "auto",
        "orientation": "auto",
        "reduceOptions": {
            "calcs": [
              "lastNotNull"
          ],
          "fields": "",
          "values": false
        },
        "text": { },
        "textMode": "auto"
      },
      "pluginVersion": "9.0.0",
      "targets": [
        {
        "datasource": {
            "type": "prometheus",
            "uid": "mimir"
          },
          "editorMode": "code",
          "exemplar": false,
          "expr": "sum(kube_pod_container_status_restarts_total{container=\"hello-world\"}) by (container) > 0",
          "format": "time_series",
          "instant": true,
          "legendFormat": "{{container}}",
          "range": false,
          "refId": "A"
        }
      ],
      "title": "Restarts",
      "type": "stat"
    },
    {
    "datasource": { },
      "fieldConfig": {
        "defaults": {
            "color": {
                "mode": "thresholds"
            },
          "decimals": 0,
          "mappings": [
            {
                "options": {
                    "match": "null",
                "result": {
                        "text": "N/A"
                }
                },
              "type": "special"
            }
          ],
          "thresholds": {
                "mode": "absolute",
            "steps": [
              {
                    "color": "green",
                "value": null
              },
              {
                    "color": "red",
                "value": 80
              }
            ]
          },
          "unit": "none"
        },
        "overrides": []
      },
      "gridPos": {
        "h": 3,
        "w": 3,
        "x": 21,
        "y": 5
      },
      "id": 12,
      "interval": "1m",
      "links": [],
      "maxDataPoints": 100,
      "options": {
        "colorMode": "none",
        "graphMode": "none",
        "justifyMode": "auto",
        "orientation": "horizontal",
        "reduceOptions": {
            "calcs": [
              "mean"
          ],
          "fields": "",
          "values": false
        },
        "textMode": "auto"
      },
      "pluginVersion": "9.0.0",
      "targets": [
        {
        "datasource": {
            "type": "prometheus",
            "uid": "cortex"
          },
          "exemplar": false,
          "expr": "sum(kube_deployment_status_replicas{deployment=\"hello-world\"})",
          "instant": true,
          "interval": "",
          "intervalFactor": 2,
          "legendFormat": "",
          "refId": "A",
          "step": 40
        }
      ],
      "title": "Total",
      "type": "stat"
    },
    {
    "datasource": {
        "type": "prometheus",
        "uid": "mimir"
      },
      "fieldConfig": {
        "defaults": {
            "color": {
                "mode": "palette-classic"
            },
          "custom": {
                "axisLabel": "",
            "axisPlacement": "auto",
            "barAlignment": 0,
            "drawStyle": "line",
            "fillOpacity": 0,
            "gradientMode": "none",
            "hideFrom": {
                    "legend": false,
              "tooltip": false,
              "viz": false
            },
            "lineInterpolation": "linear",
            "lineWidth": 1,
            "pointSize": 5,
            "scaleDistribution": {
                    "type": "linear"
            },
            "showPoints": "auto",
            "spanNulls": false,
            "stacking": {
                    "group": "A",
              "mode": "none"
            },
            "thresholdsStyle": {
                    "mode": "off"
            }
            },
          "mappings": [],
          "thresholds": {
                "mode": "absolute",
            "steps": [
              {
                    "color": "green",
                "value": null
              },
              {
                    "color": "red",
                "value": 80
              }
            ]
          }
        },
        "overrides": []
      },
      "gridPos": {
        "h": 7,
        "w": 12,
        "x": 0,
        "y": 8
      },
      "id": 16,
      "interval": "15s",
      "options": {
        "legend": {
            "calcs": [],
          "displayMode": "list",
          "placement": "bottom"
        },
        "tooltip": {
            "mode": "single",
          "sort": "none"
        }
    },
      "targets": [
        {
        "datasource": {
            "type": "prometheus",
            "uid": "cortex"
          },
          "exemplar": true,
          "expr": "sum(irate(helloworld_request_count{}[$__rate_interval]))",
          "interval": "1m",
          "intervalFactor": 1,
          "legendFormat": "{{pod}}",
          "refId": "A"
        }
      ],
      "title": "Requests (per second)",
      "type": "timeseries"
    },
    {
    "datasource": { },
      "fieldConfig": {
        "defaults": {
            "color": {
                "mode": "palette-classic"
            },
          "custom": {
                "axisLabel": "",
            "axisPlacement": "auto",
            "barAlignment": 0,
            "drawStyle": "line",
            "fillOpacity": 0,
            "gradientMode": "none",
            "hideFrom": {
                    "legend": false,
              "tooltip": false,
              "viz": false
            },
            "lineInterpolation": "linear",
            "lineWidth": 1,
            "pointSize": 5,
            "scaleDistribution": {
                    "type": "linear"
            },
            "showPoints": "auto",
            "spanNulls": false,
            "stacking": {
                    "group": "A",
              "mode": "none"
            },
            "thresholdsStyle": {
                    "mode": "off"
            }
            },
          "mappings": [],
          "thresholds": {
                "mode": "absolute",
            "steps": [
              {
                    "color": "green",
                "value": null
              },
              {
                    "color": "red",
                "value": 80
              }
            ]
          }
        },
        "overrides": []
      },
      "gridPos": {
        "h": 7,
        "w": 12,
        "x": 12,
        "y": 8
      },
      "id": 3,
      "interval": "15s",
      "options": {
        "legend": {
            "calcs": [],
          "displayMode": "list",
          "placement": "bottom"
        },
        "tooltip": {
            "mode": "single",
          "sort": "none"
        }
    },
      "targets": [
        {
        "datasource": {
            "type": "prometheus",
            "uid": "cortex"
          },
          "exemplar": true,
          "expr": "sum(irate(helloworld_request_count{}[$__rate_interval])) by (pod)",
          "interval": "1m",
          "intervalFactor": 1,
          "legendFormat": "{{pod}}",
          "refId": "A"
        }
      ],
      "title": "Requests by pod (per second)",
      "type": "timeseries"
    },
    {
    "datasource": {
        "type": "loki",
        "uid": "loki"
      },
      "gridPos": {
        "h": 6,
        "w": 24,
        "x": 0,
        "y": 15
      },
      "id": 18,
      "options": {
        "dedupStrategy": "none",
        "enableLogDetails": true,
        "prettifyLogMessage": false,
        "showCommonLabels": false,
        "showLabels": false,
        "showTime": false,
        "sortOrder": "Descending",
        "wrapLogMessage": false
      },
      "targets": [
        {
        "datasource": {
            "type": "loki",
            "uid": "loki"
          },
          "editorMode": "code",
          "expr": "{app=~\"hello-world|weather-api\", container!=\"istio-proxy\"} | pattern `[<time>] [<level>] [<version>] [<index>] <message>` | line_format `[{{.time}}] [{{.pod}}] [{{.level}}] {{.message}}` | level=~`$log_level`",
          "legendFormat": "",
          "queryType": "range",
          "refId": "A"
        }
      ],
      "title": "Logs",
      "type": "logs"
    }
  ],
  "refresh": "5s",
  "schemaVersion": 36,
  "style": "dark",
  "tags": [],
  "templating": {
    "list": [
      {
        "current": {
            "selected": true,
          "text": [
            "All"
          ],
          "value": [
            "$__all"
          ]
        },
        "hide": 0,
        "includeAll": true,
        "label": "Log Level",
        "multi": true,
        "name": "log_level",
        "options": [
          {
            "selected": true,
            "text": "All",
            "value": "$__all"
          },
          {
            "selected": false,
            "text": "INFO",
            "value": "INFO"
          },
          {
            "selected": false,
            "text": "ERROR",
            "value": "ERROR"
          },
          {
            "selected": false,
            "text": "DEBUG",
            "value": "DEBUG"
          },
          {
            "selected": false,
            "text": "TRACE",
            "value": "TRACE"
          },
          {
            "selected": false,
            "text": "WARN",
            "value": "WARN"
          }
        ],
        "query": "INFO, ERROR, DEBUG, TRACE, WARN",
        "queryValue": "",
        "skipUrlSync": false,
        "type": "custom"
      }
    ]
  },
  "time": {
    "from": "now-5m",
    "to": "now"
  },
  "timepicker": {
    "refresh_intervals": [
      "5s",
      "15s",
      "30s",
      "1m",
      "5m",
      "15m",
      "30m",
      "1h",
      "2h",
      "1d"
    ]
  },
  "timezone": "",
  "title": "Hello, World!",
  "uid": "dWgz0Uq7k",
  "version": 3,
  "weekStart": ""
}
""";
    }
}