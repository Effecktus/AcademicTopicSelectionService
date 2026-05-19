#!/bin/sh
set -e
ACCESS=$(cat /run/secrets/minio_access_key | tr -d '\015\012')
SECRET=$(cat /run/secrets/minio_secret_key | tr -d '\015\012')
mc alias set local http://minio:9000 "$ACCESS" "$SECRET"
mc mb --ignore-existing local/graduate-works
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_01/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_02/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_03/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_04/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_05/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_06/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_07/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_08/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_09/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2024/work_10/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_11/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_12/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_13/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_14/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_15/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_16/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_17/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_18/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_19/thesis.pdf
mc cp /seed-files/thesis.pdf local/graduate-works/vkr/2025/work_20/thesis.pdf
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2024/work_02/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2024/work_04/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2024/work_06/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2024/work_08/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2024/work_10/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2025/work_12/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2025/work_14/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2025/work_16/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2025/work_18/presentation.pptx
mc cp /seed-files/presentation.pptx local/graduate-works/vkr/2025/work_20/presentation.pptx
echo "MinIO seed: done."