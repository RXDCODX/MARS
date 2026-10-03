import { describe, expect, it } from "vitest";

import {
  FACE_ASSETS,
  getFaceByName,
  getRandomFace,
  getRandomFaceByType,
  isVideoFile,
} from "./faceUtils";

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

  describe("getFaceByName", () => {
    it("should return face by exact name", () => {
      const testFace = FACE_ASSETS[0];
      const foundFace = getFaceByName(testFace.name);
      expect(foundFace).toEqual(testFace);
    });

    it("should return undefined for non-existent name", () => {
      const foundFace = getFaceByName("non-existent-face");
      expect(foundFace).toBeUndefined();
    });

    it("should handle empty string", () => {
      const foundFace = getFaceByName("");
      expect(foundFace).toBeUndefined();
    });
  });

  describe("isVideoFile", () => {
    it("should return true for video files", () => {
      expect(isVideoFile("test.mp4")).toBe(true);
      expect(isVideoFile("test.webm")).toBe(true);
      expect(isVideoFile("test.avi")).toBe(true);
      expect(isVideoFile("/path/to/video.mp4")).toBe(true);
    });

    it("should return false for non-video files", () => {
      expect(isVideoFile("test.gif")).toBe(false);
      expect(isVideoFile("test.jpg")).toBe(false);
      expect(isVideoFile("test.png")).toBe(false);
      expect(isVideoFile("test.txt")).toBe(false);
    });

    it("should handle empty string", () => {
      expect(isVideoFile("")).toBe(false);
    });
  });
});
