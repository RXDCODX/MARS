import { SearchOutlined } from "@ant-design/icons";
import { Button, Card, Flex, Input, Tag, Typography } from "antd";
import { Suspense, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { allRoutes, RouteConfig } from "@/routes/config";
import { reactBitsBackgroundComponentRegistry } from "@/shared/components/ReactBitsBackgroundsLegacy/registry";

const GridMotionBg = reactBitsBackgroundComponentRegistry.GridMotion;

const typeLabels: Record<string, string> = {
  site: "Страницы сайта",
  obs: "OBS Компоненты",
  "control panel": "Панель управления",
  special: "Специальные",
};

const typeIcons: Record<string, string> = {
  site: "🌐",
  obs: "📹",
  "control panel": "⚙️",
  special: "⭐",
};

const typeColors: Record<string, string> = {
  site: "blue",
  obs: "green",
  "control panel": "orange",
  special: "purple",
};

const RoutesPage: React.FC = () => {
  const [activeFilter, setActiveFilter] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState("");

  const types = useMemo(() => {
    const uniqueTypes = [...new Set(allRoutes.map(r => r.type))];
    return ["all", ...uniqueTypes];
  }, []);

  const filteredRoutes = useMemo(() => {
    let routes = allRoutes;
    if (activeFilter !== "all") {
      routes = routes.filter(r => r.type === activeFilter);
    }
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      routes = routes.filter(
        r =>
          r.name.toLowerCase().includes(query) ||
          r.path.toLowerCase().includes(query)
      );
    }
    return routes;
  }, [activeFilter, searchQuery]);

  const groupedFiltered = useMemo(
    () =>
      filteredRoutes.reduce(
        (accumulator, route) => {
          if (!accumulator[route.type]) accumulator[route.type] = [];
          accumulator[route.type].push(route);
          return accumulator;
        },
        {} as Record<string, RouteConfig[]>
      ),
    [filteredRoutes]
  );

  const typeCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const route of allRoutes) {
      counts[route.type] = (counts[route.type] || 0) + 1;
    }
    return counts;
  }, []);

  return (
    <div
      style={{ position: "relative", minHeight: "100vh" }}
      data-testid="routes-page"
    >
      <div style={{ position: "fixed", inset: 0, zIndex: -10, opacity: 0.3 }}>
        <Suspense fallback={null}>
          <GridMotionBg style={{ position: "absolute", inset: 0 }} />
        </Suspense>
      </div>

      <div style={{ maxWidth: 1200, margin: "0 auto", padding: "32px 24px" }}>
        <Flex align="center" gap={12} style={{ marginBottom: 24 }}>
          <Typography.Title level={2} style={{ margin: 0 }}>
            Все маршруты
          </Typography.Title>
          <Tag color="purple">{allRoutes.length}</Tag>
        </Flex>

        <Flex
          justify="space-between"
          align="center"
          wrap="wrap"
          gap={12}
          style={{ marginBottom: 24 }}
        >
          <Flex gap={6} wrap="wrap" data-testid="filter-tabs">
            {types.map(type => (
              <Button
                key={type}
                size="small"
                type={activeFilter === type ? "primary" : "default"}
                onClick={() => setActiveFilter(type)}
                data-testid={`filter-tab-${type}`}
              >
                {type === "all" ? (
                  "Все"
                ) : (
                  <>
                    <span style={{ marginRight: 4 }}>
                      {typeIcons[type] || "📄"}
                    </span>
                    {typeLabels[type] || type}
                    <Tag
                      color={typeColors[type] || "default"}
                      style={{ marginLeft: 4, fontSize: 10 }}
                    >
                      {typeCounts[type] || 0}
                    </Tag>
                  </>
                )}
              </Button>
            ))}
          </Flex>

          <Input
            placeholder="Поиск маршрутов..."
            prefix={<SearchOutlined />}
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            allowClear
            style={{ width: 240 }}
            data-testid="search-input"
          />
        </Flex>

        {filteredRoutes.length === 0 ? (
          <Flex
            vertical
            align="center"
            justify="center"
            gap={12}
            style={{ padding: "64px 0" }}
          >
            <Typography.Text style={{ fontSize: 32, opacity: 0.5 }}>
              🔍
            </Typography.Text>
            <Typography.Text type="secondary">
              Маршруты не найдены
            </Typography.Text>
          </Flex>
        ) : (
          <Flex vertical gap={24}>
            {Object.entries(groupedFiltered).map(([type, routes]) => (
              <div key={type}>
                <Flex align="center" gap={8} style={{ marginBottom: 12 }}>
                  <span style={{ fontSize: 18 }}>
                    {typeIcons[type] || "📄"}
                  </span>
                  <Typography.Title level={5} style={{ margin: 0 }}>
                    {typeLabels[type] || type}
                  </Typography.Title>
                  <Tag color={typeColors[type] || "default"}>
                    {routes.length}
                  </Tag>
                </Flex>
                <Flex gap={8} wrap="wrap">
                  {routes.map((route, index) => (
                    <Link
                      key={index}
                      to={route.path}
                      style={{
                        flex: "1 1 280px",
                        maxWidth: "100%",
                        textDecoration: "none",
                      }}
                      data-testid={`route-card-${route.path.replaceAll("/", "-").replace(/^-/, "")}`}
                    >
                      <Card hoverable size="small" style={{ height: "100%" }}>
                        <Flex justify="space-between" align="center">
                          <Flex
                            vertical
                            gap={2}
                            style={{ minWidth: 0, flex: 1 }}
                          >
                            <Typography.Text
                              strong
                              style={{ fontSize: 14 }}
                              ellipsis
                            >
                              {route.name || route.path}
                            </Typography.Text>
                            <Typography.Text
                              type="secondary"
                              code
                              style={{ fontSize: 12 }}
                              ellipsis
                            >
                              {route.path}
                            </Typography.Text>
                          </Flex>
                          <Typography.Text
                            type="secondary"
                            style={{
                              marginLeft: 8,
                              fontSize: 16,
                              opacity: 0.3,
                            }}
                          >
                            →
                          </Typography.Text>
                        </Flex>
                      </Card>
                    </Link>
                  ))}
                </Flex>
              </div>
            ))}
          </Flex>
        )}

        <Flex
          justify="center"
          gap={24}
          wrap="wrap"
          style={{
            marginTop: 32,
            padding: "16px 24px",
            borderRadius: 16,
            border: "3px solid var(--site-border-primary)",
            backgroundColor: "var(--site-bg-secondary)",
          }}
        >
          {Object.entries(typeCounts).map(([type, count]) => (
            <Flex key={type} align="center" gap={6}>
              <span>{typeIcons[type] || "📄"}</span>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {typeLabels[type] || type}
              </Typography.Text>
              <Typography.Text strong style={{ fontSize: 14 }}>
                {count}
              </Typography.Text>
            </Flex>
          ))}
        </Flex>
      </div>
    </div>
  );
};

export default RoutesPage;
