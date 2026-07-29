{{/*
Resource names in this chart are intentionally fixed (dataaccess, management,
gateway, mysql, jobmonitor, microservice0) rather than {{ .Release.Name }}-
prefixed like a typical Helm chart's fullname template - services reference
each other by these exact in-cluster DNS names (e.g. management-config's
GrpcClients__UserRepositoryService points at
dataaccess.microservice0.svc.cluster.local), so templating the names would
break those cross-references. This is the one deliberate deviation from
Helm's usual naming convention in this chart - there is no
"microservice0.fullname" helper because nothing here is meant to vary by
release name.
*/}}

{{/*
Common labels for one component. Usage:
  {{- include "microservice0.labels" (dict "name" "dataaccess" "component" "backend") | nindent 4 }}
*/}}
{{- define "microservice0.labels" -}}
app.kubernetes.io/name: {{ .name }}
app.kubernetes.io/part-of: microservice0
app.kubernetes.io/component: {{ .component }}
{{- end -}}
