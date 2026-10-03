import { Tabs } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

import { RandomMeme } from "@/shared/api";
import { MemeOrderDto, MemeTypeDto } from "@/shared/api";
import { useToastModal } from "@/shared/Utils/ToastModal";

import {
  DeleteConfirmationModal,
  RandomMemeDetails,
  RandomMemeForm,
  RandomMemeList,
  RandomMemeTypesList,
} from "./components";
import styles from "./RandomMemePage.module.scss";
import {
  MemeOrderFormData,
  MemeTypeFormData,
  NavigationState,
  RandomMemeView,
} from "./RandomMemePage.types";

const RandomMemePage: React.FC = () => {
  const api = useMemo(() => new RandomMeme(), []);
  const { showToast } = useToastModal();
  const navigate = useNavigate();

  const [navigation, setNavigation] = useState<NavigationState>({
    currentView: "list",
    selectedType: null,
    selectedOrder: null,
    isLoading: false,
    error: "",
  });

  const [memeTypes, setMemeTypes] = useState<MemeTypeDto[]>([]);
  const [memeOrders, setMemeOrders] = useState<MemeOrderDto[]>([]);
  const [activeTab, setActiveTab] = useState<"types" | "orders">("orders");

  const [deleteModal, setDeleteModal] = useState<{
    isOpen: boolean;
    itemType: "type" | "order";
    itemName: string;
    onConfirm: () => Promise<void>;
  }>({
    isOpen: false,
    itemType: "order",
    itemName: "",
    onConfirm: async () => {},
  });

  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadTypes = useCallback(async () => {
    try {
      setNavigation(previous => ({ ...previous, isLoading: true, error: "" }));
      const response = await api.randomMemeTypesList();
      setMemeTypes(response.data.data ?? []);
    } catch (error) {
      console.error("Ошибка загрузки типов:", error);
      const errorMessage =
        error instanceof Error ? error.message : "Неизвестная ошибка";
      setNavigation(previous => ({ ...previous, error: errorMessage }));
      showToast({
        success: false,
        message: errorMessage,
      });
    } finally {
      setNavigation(previous => ({ ...previous, isLoading: false }));
    }
  }, [api, showToast]);

  const loadOrders = useCallback(async () => {
    try {
      setNavigation(previous => ({ ...previous, isLoading: true, error: "" }));
      const response = await api.randomMemeOrdersList();
      setMemeOrders(response.data.data ?? []);
    } catch (error) {
      console.error("Ошибка загрузки заказов:", error);
      const errorMessage =
        error instanceof Error ? error.message : "Неизвестная ошибка";
      setNavigation(previous => ({ ...previous, error: errorMessage }));
      showToast({
        success: false,
        message: errorMessage,
      });
    } finally {
      setNavigation(previous => ({ ...previous, isLoading: false }));
    }
  }, [api, showToast]);

  useEffect(() => {
    loadTypes();
    loadOrders();
  }, [loadTypes, loadOrders]);

  const navigateTo = useCallback(
    (
      view: RandomMemeView,
      selectedType?: MemeTypeDto,
      selectedOrder?: MemeOrderDto
    ) => {
      setNavigation(previous => ({
        ...previous,
        currentView: view,
        selectedType: selectedType || null,
        selectedOrder: selectedOrder || null,
        error: "",
      }));
    },
    []
  );

  const navigateBack = useCallback(() => {
    setNavigation(previous => ({
      ...previous,
      currentView: "list",
      selectedType: null,
      selectedOrder: null,
      error: "",
    }));
  }, []);

  const handleTypeViewDetails = useCallback(
    (memeType: MemeTypeDto) => {
      navigateTo("type-details", memeType);
    },
    [navigateTo]
  );

  const handleTypeEdit = useCallback(
    (memeType: MemeTypeDto) => {
      navigateTo("edit-type", memeType);
    },
    [navigateTo]
  );

  const handleTypeDelete = useCallback(
    (memeType: MemeTypeDto) => {
      setDeleteModal({
        isOpen: true,
        itemType: "type",
        itemName: memeType.name,
        onConfirm: async () => {
          try {
            const result = await api.randomMemeTypesDelete(memeType.id);
            showToast(result.data);
            await loadTypes();
            setDeleteModal(previous => ({ ...previous, isOpen: false }));
          } catch (error) {
            console.error("Ошибка удаления типа:", error);
            showToast({
              success: false,
              message: "Ошибка удаления типа мема",
            });
          }
        },
      });
    },
    [api, loadTypes, showToast]
  );

  const handleTypeCreate = useCallback(() => {
    navigateTo("create-type");
  }, [navigateTo]);

  const handleOrderViewDetails = useCallback(
    (memeOrder: MemeOrderDto) => {
      navigate(`/random-meme/${memeOrder.id}`);
    },
    [navigate]
  );

  const handleOrderEdit = useCallback(
    (memeOrder: MemeOrderDto) => {
      navigate(`/random-meme/edit/${memeOrder.id}`);
    },
    [navigate]
  );

  const handleOrderDelete = useCallback(
    (memeOrder: MemeOrderDto) => {
      setDeleteModal({
        isOpen: true,
        itemType: "order",
        itemName: `Заказ #${memeOrder.order}`,
        onConfirm: async () => {
          try {
            const result = await api.randomMemeOrdersDelete(memeOrder.id);
            showToast(result.data);
            await loadOrders();
            setDeleteModal(previous => ({ ...previous, isOpen: false }));
          } catch (error) {
            console.error("Ошибка удаления заказа:", error);
            showToast({
              success: false,
              message: "Ошибка удаления заказа мема",
            });
          }
        },
      });
    },
    [api, loadOrders, showToast]
  );

  const handleOrderCreate = useCallback(() => {
    navigateTo("create-order");
  }, [navigateTo]);

  const handleFormSubmit = useCallback(
    async (data: Record<string, unknown>) => {
      try {
        setIsSubmitting(true);

        if (navigation.currentView.includes("type")) {
          const typeData = data as unknown as MemeTypeFormData;
          if (navigation.currentView === "create-type") {
            const result = await api.randomMemeTypesCreate(typeData);
            showToast(result.data);
          } else {
            const result = await api.randomMemeTypesUpdate(
              navigation.selectedType!.id,
              typeData
            );
            showToast(result.data);
          }
          await loadTypes();
        } else {
          const orderData = data as unknown as MemeOrderFormData;
          if (navigation.currentView === "create-order") {
            const result = await api.randomMemeOrdersCreate(orderData);
            showToast(result.data);
          } else {
            const result = await api.randomMemeOrdersUpdate(
              navigation.selectedOrder!.id,
              orderData
            );
            showToast(result.data);
          }
          await loadOrders();
        }

        navigateBack();
      } catch (error) {
        console.error("Ошибка сохранения:", error);
        showToast({
          success: false,
          message: "Ошибка сохранения данных",
        });
      } finally {
        setIsSubmitting(false);
      }
    },
    [navigation, api, loadTypes, loadOrders, navigateBack, showToast]
  );

  const renderCurrentView = () => {
    switch (navigation.currentView) {
      case "list": {
        return (
          <Tabs
            activeKey={activeTab}
            onChange={key => setActiveTab(key as "types" | "orders")}
            style={{ marginBottom: 32 }}
            items={[
              {
                key: "orders",
                label: `Заказы мемов (${memeOrders.length})`,
                children: (
                  <RandomMemeList
                    memeOrders={memeOrders}
                    memeTypes={memeTypes}
                    isLoading={navigation.isLoading}
                    error={navigation.error}
                    onRefresh={loadOrders}
                    onViewDetails={handleOrderViewDetails}
                    onEdit={handleOrderEdit}
                    onDelete={handleOrderDelete}
                    onCreate={handleOrderCreate}
                    showToast={showToast}
                  />
                ),
              },
              {
                key: "types",
                label: `Типы мемов (${memeTypes.length})`,
                children: (
                  <RandomMemeTypesList
                    memeTypes={memeTypes}
                    isLoading={navigation.isLoading}
                    error={navigation.error}
                    onRefresh={loadTypes}
                    onViewDetails={handleTypeViewDetails}
                    onEdit={handleTypeEdit}
                    onDelete={handleTypeDelete}
                    onCreate={handleTypeCreate}
                  />
                ),
              },
            ]}
          />
        );
      }

      case "type-details": {
        return navigation.selectedType ? (
          <RandomMemeDetails
            memeType={navigation.selectedType}
            isLoading={navigation.isLoading}
            onBack={navigateBack}
            onEdit={() => navigateTo("edit-type", navigation.selectedType!)}
            onDelete={() => handleTypeDelete(navigation.selectedType!)}
            onRefresh={loadTypes}
          />
        ) : null;
      }

      case "create-type":
      case "edit-type": {
        return (
          <RandomMemeForm
            memeType={navigation.selectedType}
            memeTypes={memeTypes}
            isSubmitting={isSubmitting}
            mode={navigation.currentView === "create-type" ? "create" : "edit"}
            onSubmit={handleFormSubmit}
            onCancel={navigateBack}
          />
        );
      }

      case "create-order": {
        return (
          <RandomMemeForm
            memeOrder={navigation.selectedOrder}
            memeTypes={memeTypes}
            isSubmitting={isSubmitting}
            mode="create"
            onSubmit={handleFormSubmit}
            onCancel={navigateBack}
          />
        );
      }

      default: {
        return null;
      }
    }
  };

  return (
    <div className={styles.randomMemePage}>
      {renderCurrentView()}

      <DeleteConfirmationModal
        isOpen={deleteModal.isOpen}
        itemType={deleteModal.itemType}
        itemName={deleteModal.itemName}
        isDeleting={isSubmitting}
        onConfirm={deleteModal.onConfirm}
        onCancel={() =>
          setDeleteModal(previous => ({ ...previous, isOpen: false }))
        }
      />
    </div>
  );
};

export default RandomMemePage;
