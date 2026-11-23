import { lazy } from "react";

const AllRefundLoader = () => import("@/overlays/AllRefund");
export const AllRefundManager = lazy(AllRefundLoader);
