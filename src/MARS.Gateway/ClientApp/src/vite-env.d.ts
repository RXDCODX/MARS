/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_BASE_PATH: string;
  readonly VITE_API_BASE_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

declare module "*.webm" {
  const source: string;
  export default source;
}

declare module "*.mp4" {
  const source: string;
  export default source;
}

declare module "*.module.scss" {
  const classes: { [key: string]: string };
  export default classes;
}
