import ServiceDetails from "@/components/Site/Pages/ServerViewer/ServiceDetails";

// Компонент-обертка для страницы деталей сервиса
const ServiceDetailsPage = () => (
  <div style={{ padding: "20px" }}>
    <ServiceDetails onClose={() => history.back()} />
  </div>
);

export default ServiceDetailsPage;
