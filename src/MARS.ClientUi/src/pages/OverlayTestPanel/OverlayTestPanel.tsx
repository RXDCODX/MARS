import { useCallback, useMemo, useState } from "react";
import {
  Alert,
  Button,
  Card,
  Collapse,
  Input,
  Space,
  Tag,
  Typography,
} from "antd";

import { defaultApiConfig } from "@/shared/api/api-config";
import { OverlayTest } from "@/shared/api/http-clients/OverlayTest";
import { obsComponentRoutes } from "@/routes/config/obsComponentRoutes";

import { OVERLAY_TEMPLATES, type OverlayTemplate } from "./templates";

const { Text, Title, Paragraph } = Typography;
const { TextArea } = Input;

interface SendState {
  status: "idle" | "sending" | "ok" | "error";
  message?: string;
}

function formatPayload(payload: unknown | null): string {
  if (payload === null) return "";
  return JSON.stringify(payload, null, 2);
}

function TemplateCard({
  template,
  onSend,
}: {
  template: OverlayTemplate;
  onSend: (event: string, payloadJson: string | null) => Promise<void>;
}) {
  const [payloadText, setPayloadText] = useState(() => formatPayload(template.buildPayload()));
  const [state, setState] = useState<SendState>({ status: "idle" });

  const regenerate = () => {
    setPayloadText(formatPayload(template.buildPayload()));
    setState({ status: "idle" });
  };

  const send = async () => {
    setState({ status: "sending" });
    try {
      const trimmed = payloadText.trim();
      if (trimmed) {
        JSON.parse(trimmed);
      }
      await onSend(template.event, template.buildPayload() === null ? null : (trimmed || null));
      setState({ status: "ok", message: "Отправлено" });
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      setState({ status: "error", message: msg });
    }
  };

  const isParameterless = template.buildPayload() === null;

  return (
    <Card
      size="small"
      title={
        <Space wrap>
          <span>{template.title}</span>
          <Tag color="blue">{template.event}</Tag>
        </Space>
      }
      extra={
        <Space>
          {!isParameterless && (
            <Button size="small" onClick={regenerate}>
              Перегенерировать
            </Button>
          )}
          <Button size="small" type="primary" loading={state.status === "sending"} onClick={() => void send()}>
            Отправить
          </Button>
        </Space>
      }
      style={{ marginBottom: 12 }}
    >
      <Paragraph type="secondary" style={{ marginBottom: 8 }}>
        {template.description}
      </Paragraph>
      <Space wrap style={{ marginBottom: 8 }}>
        {template.overlayRoutes.map(route => (
          <a key={route} href={route} target="_blank" rel="noreferrer">
            <Tag color="green">{route}</Tag>
          </a>
        ))}
      </Space>
      {!isParameterless && (
        <TextArea
          value={payloadText}
          onChange={e => {
            setPayloadText(e.target.value);
            if (state.status !== "idle") setState({ status: "idle" });
          }}
          autoSize={{ minRows: 3, maxRows: 18 }}
          style={{ fontFamily: "monospace", fontSize: 12 }}
        />
      )}
      {state.status === "ok" && <Alert style={{ marginTop: 8 }} type="success" showIcon message={state.message} />}
      {state.status === "error" && <Alert style={{ marginTop: 8 }} type="error" showIcon message={state.message} />}
    </Card>
  );
}

export default function OverlayTestPanel() {
  const client = useMemo(() => new OverlayTest(defaultApiConfig), []);
  const [globalError, setGlobalError] = useState<string | null>(null);

  const onSend = useCallback(
    async (event: string, payloadJson: string | null) => {
      setGlobalError(null);
      try {
        await client.overlayTestEventCreate({ eventName: event, payloadJson });
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        setGlobalError(msg);
        throw e;
      }
    },
    [client]
  );

  const overlayCoverage = useMemo(() => {
    const eventByRoute = new Map<string, string[]>();
    for (const t of OVERLAY_TEMPLATES) {
      for (const r of t.overlayRoutes) {
        const arr = eventByRoute.get(r) ?? [];
        arr.push(t.event);
        eventByRoute.set(r, arr);
      }
    }
    return obsComponentRoutes.map(r => ({
      path: r.path,
      name: r.name ?? r.path,
      events: eventByRoute.get(r.path) ?? [],
    }));
  }, []);

  return (
    <div style={{ padding: 24 }}>
      <Title level={2}>Пульт тестирования оверлеев</Title>
      <Paragraph>
        Отправляет фейковые события через Gateway → <Text code>/api/overlay-test/event</Text> → MARS.Alerts → SignalR{" "}
        <Text code>/hubs/overlay</Text>. Откройте нужный оверлей в другой вкладке/OBS и нажмите «Отправить».
        Для звука кликните по странице оверлея (браузерный autoplay policy).
      </Paragraph>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Требуется OverlayDevTools:Enabled=true на MARS.Alerts (в dev включено по умолчанию)."
      />
      {globalError && <Alert type="error" showIcon style={{ marginBottom: 16 }} message={globalError} />}

      <Collapse
        style={{ marginBottom: 16 }}
        items={[
          {
            key: "coverage",
            label: `Покрытие оверлеев (${overlayCoverage.length} маршрутов)`,
            children: (
              <Space wrap>
                {overlayCoverage.map(o => (
                  <Tag key={o.path} color={o.events.length > 0 ? "green" : "default"}>
                    {o.path}
                    {o.events.length > 0 ? ` (${o.events.length})` : " — без хаба"}
                  </Tag>
                ))}
              </Space>
            ),
          },
        ]}
      />

      {OVERLAY_TEMPLATES.map(t => (
        <TemplateCard key={t.event} template={t} onSend={onSend} />
      ))}
    </div>
  );
}
