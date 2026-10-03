import { lazy } from "react";

const AllRefundLoader = () => import("@/components/OBS_Components/AllRefund");
export const AllRefundManager = lazy(AllRefundLoader);
