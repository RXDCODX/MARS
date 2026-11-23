import { describe, expect, it } from "vitest";

import { FACE_ASSETS, getRandomFace, getRandomFaceByType } from "./faceUtils";

describe("Face Utils", () => {
  describe("FACE_ASSETS", () => {
    it("should contain face assets", () => {
      expect(FACE_ASSETS).toBeDefined();
      expect(Array.isArray(FACE_ASSETS)).toBe(true);
      expect(FACE_ASSETS.length).toBeGreaterThan(0);
    });

    it("should have correct structure for each asset", () => {
      FACE_ASSETS.forEach(asset => {
        expect(asset).toHaveProperty("name");
        expect(asset).toHaveProperty("url");
        expect(asset).toHaveProperty("type");
        expect(asset).toHaveProperty("extension");
        expect(["image", "video"]).toContain(asset.type);
      });
    });

    it("should have correct file extensions", () => {
      FACE_ASSETS.forEach(asset => {
        if (asset.type === "image") {
          expect(asset.extension).toBe(".gif");
        } else if (asset.type === "video") {
          expect(asset.extension).toBe(".mp4");
        }
      });
    });

    it("берёт URL из бандла, а не пишет путь к исходнику строкой", () => {
      // Регрессия: URL были литералами `/src/assets/faces/…`. В `yarn dev` их
      // отдаёт сам Vite, а в production-сборке файла в бандле нет и nginx
      // отвечает индексом. `<img>` с `nosniff` получает HTML, стреляет `error`
      // вместо `load`, и на `/highlite` сообщение залипало навсегда.
      //
      // Форму URL здесь не проверяем: vitest отдаёт ассеты из исходников, и
      // `/src/…` в нём — честный результат, а не признак дефекта. Настоящую
      // проверку даёт сборка: файлы обязаны появиться в `dist/assets`.
      FACE_ASSETS.forEach(asset => {
        expect(asset.url).not.toBe("");
      });
    });

    it("не содержит имён с дубликатом перетаскивания", () => {
      // `marin-kitagawa (1).gif` — случайная копия. В ротации её не было, и
      // молча добавлять её тоже нельзя: от неё зависит то, что видит зритель.
      const names = FACE_ASSETS.map(asset => asset.name);

      expect(names).not.toContain("marin-kitagawa (1)");
      expect(new Set(names).size).toBe(names.length);
    });
  });

  describe("getRandomFace", () => {
    it("should return a random face asset", () => {
      const face = getRandomFace();
      expect(face).toBeDefined();
      expect(FACE_ASSETS).toContain(face);
    });

    it("should return different faces on multiple calls", () => {
      const faces = new Set();
      for (let index = 0; index < 10; index++) {
        faces.add(getRandomFace().name);
      }
      // В идеале должны быть разные лица, но это не гарантировано
      expect(faces.size).toBeGreaterThan(0);
    });
  });

  describe("getRandomFaceByType", () => {
    it("should return only image faces when type is image", () => {
      const imageFace = getRandomFaceByType("image");
      expect(imageFace.type).toBe("image");
      expect(imageFace.extension).toBe(".gif");
    });

    it("should return only video faces when type is video", () => {
      const videoFace = getRandomFaceByType("video");
      expect(videoFace.type).toBe("video");
      expect(videoFace.extension).toBe(".mp4");
    });

    it("should return valid face from FACE_ASSETS", () => {
      const imageFace = getRandomFaceByType("image");
      const videoFace = getRandomFaceByType("video");

      expect(FACE_ASSETS).toContain(imageFace);
      expect(FACE_ASSETS).toContain(videoFace);
    });
  });
});
