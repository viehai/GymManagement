using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GymManagement.Models;

namespace GymManagement.Helpers
{
    /// <summary>
    /// Tiện ích toán học xử lý vector đặc trưng khuôn mặt (FaceNet 128D Embedding) và so khớp danh tính.
    /// </summary>
    public static class FaceRecognitionHelper
    {
        /// <summary>
        /// Ngưỡng khoảng cách Euclidean tối đa để công nhận 2 khuôn mặt là cùng 1 người.
        /// FaceNet chuẩn sử dụng ngưỡng 0.50 - 0.54 cho camera webcam laptop có biến động ánh sáng và góc nghiêng.
        /// </summary>
        public const double DefaultDistanceThreshold = 0.53;

        /// <summary>
        /// Chuyển đổi chuỗi JSON chứa mảng float 128 chiều thành float[]
        /// </summary>
        public static float[]? ParseEmbedding(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var arr = JsonSerializer.Deserialize<float[]>(json);
                if (arr == null || arr.Length != 128) return null;
                return arr;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tính khoảng cách Euclidean giữa 2 vector đặc trưng 128D.
        /// Khoảng cách càng nhỏ (gần 0) thì 2 khuôn mặt càng giống nhau.
        /// </summary>
        public static double CalculateEuclideanDistance(float[] v1, float[] v2)
        {
            if (v1 == null || v2 == null || v1.Length != v2.Length)
                return double.MaxValue;

            double sum = 0.0;
            for (int i = 0; i < v1.Length; i++)
            {
                double diff = v1[i] - v2[i];
                sum += diff * diff;
            }

            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Tính độ tương đồng Cosine (Cosine Similarity) giữa 2 vector.
        /// Giá trị từ -1.0 đến 1.0 (1.0 = giống hệt nhau).
        /// </summary>
        public static double CalculateCosineSimilarity(float[] v1, float[] v2)
        {
            if (v1 == null || v2 == null || v1.Length != v2.Length)
                return 0.0;

            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < v1.Length; i++)
            {
                dotProduct += v1[i] * v2[i];
                normA += v1[i] * v1[i];
                normB += v2[i] * v2[i];
            }

            if (normA <= 0.0 || normB <= 0.0) return 0.0;
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        /// <summary>
        /// Quy đổi khoảng cách Euclidean sang % điểm tin cậy (Confidence Score 0.0 - 1.0).
        /// Khoảng cách 0.20 -> 95%+, 0.40 -> 85%, 0.52 -> 75%, >= 0.65 -> 0%
        /// </summary>
        public static double DistanceToConfidenceScore(double distance)
        {
            if (distance <= 0) return 1.0;
            if (distance >= 0.65) return 0.0;
            double score = 1.0 - (distance / 0.65) * 0.35;
            return Math.Round(Math.Clamp(score, 0.50, 0.99), 3);
        }

        /// <summary>
        /// So khớp 1 vector ứng viên với danh sách các hồ sơ khuôn mặt đã lưu.
        /// Tìm ra người trùng khớp nhất (khoảng cách nhỏ nhất và <= ngưỡng cho phép).
        /// </summary>
        public static (MemberFaceProfile Profile, double Distance, double ConfidenceScore)? FindBestMatch(
            float[] candidateVector,
            IEnumerable<MemberFaceProfile> existingProfiles,
            double threshold = DefaultDistanceThreshold)
        {
            if (candidateVector == null || candidateVector.Length != 128)
                return null;

            MemberFaceProfile? bestProfile = null;
            double minDistance = double.MaxValue;

            foreach (var profile in existingProfiles)
            {
                if (!profile.IsActive) continue;

                var profileVector = ParseEmbedding(profile.FaceEmbeddingJson);
                if (profileVector == null) continue;

                double dist = CalculateEuclideanDistance(candidateVector, profileVector);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestProfile = profile;
                }
            }

            if (bestProfile != null && minDistance <= threshold)
            {
                double score = DistanceToConfidenceScore(minDistance);
                return (bestProfile, minDistance, score);
            }

            return null;
        }
    }
}
