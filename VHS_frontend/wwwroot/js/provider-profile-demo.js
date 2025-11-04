/**
 * Provider Profile Demo - Test API calls
 * File này chứa các function demo để test API Provider Profile
 */

// Demo configuration - Sử dụng localStorage như các service khác
const DEMO_CONFIG = {
    // Không cần baseURL vì service tự lấy từ window.location.origin
    // Không cần accountId vì service tự lấy từ localStorage
    // Không cần token vì service tự lấy từ localStorage
};

/**
 * Demo: Test lấy thông tin profile
 */
async function demoGetProfile() {
    console.log('🚀 Demo: Lấy thông tin profile...');
    
    try {
        const service = new ProviderProfileService();
        const profile = await service.getProfile();
        
        console.log('✅ Thành công:', profile);
        return profile;
    } catch (error) {
        console.error('❌ Lỗi:', error);
        throw error;
    }
}

/**
 * Demo: Test cập nhật profile
 */
async function demoUpdateProfile() {
    console.log('🚀 Demo: Cập nhật profile...');
    
    const updateData = {
        providerName: 'Công ty ABC Demo',
        phoneNumber: '0123456789',
        description: 'Đây là mô tả demo cho công ty ABC',
        images: 'https://example.com/demo-logo.jpg'
    };
    
    try {
        const service = new ProviderProfileService();
        const result = await service.updateProfile(updateData);
        
        console.log('✅ Cập nhật thành công:', result);
        return result;
    } catch (error) {
        console.error('❌ Lỗi cập nhật:', error);
        throw error;
    }
}

/**
 * Demo: Test lấy Provider ID
 */
async function demoGetProviderId() {
    console.log('🚀 Demo: Lấy Provider ID...');
    
    try {
        const service = new ProviderProfileService();
        const providerId = await service.getProviderId();
        
        console.log('✅ Provider ID:', providerId);
        return providerId;
    } catch (error) {
        console.error('❌ Lỗi lấy Provider ID:', error);
        throw error;
    }
}

/**
 * Demo: Test toàn bộ workflow
 */
async function demoFullWorkflow() {
    console.log('🎯 Demo: Test toàn bộ workflow...');
    
    try {
        // 1. Lấy thông tin profile
        console.log('\n--- Bước 1: Lấy thông tin profile ---');
        const profile = await demoGetProfile();
        
        // 2. Lấy Provider ID
        console.log('\n--- Bước 2: Lấy Provider ID ---');
        const providerId = await demoGetProviderId();
        
        // 3. Cập nhật profile
        console.log('\n--- Bước 3: Cập nhật profile ---');
        const updateResult = await demoUpdateProfile();
        
        // 4. Lấy lại thông tin để verify
        console.log('\n--- Bước 4: Verify cập nhật ---');
        const updatedProfile = await demoGetProfile();
        
        console.log('🎉 Demo hoàn thành thành công!');
        return {
            originalProfile: profile,
            providerId: providerId,
            updateResult: updateResult,
            updatedProfile: updatedProfile
        };
        
    } catch (error) {
        console.error('💥 Demo thất bại:', error);
        throw error;
    }
}

/**
 * Demo: Test error handling
 */
async function demoErrorHandling() {
    console.log('🚨 Demo: Test xử lý lỗi...');
    
    // Test với token không hợp lệ (giả lập)
    localStorage.setItem('vhs_token', 'invalid-token');
    const invalidService = new ProviderProfileService();
    
    try {
        await invalidService.getProfile();
    } catch (error) {
        console.log('✅ Đã bắt được lỗi token không hợp lệ:', error.message);
    }
    
    // Test với accountId không tồn tại (giả lập)
    localStorage.setItem('vhs_account_id', 'invalid-account-id');
    try {
        const service = new ProviderProfileService();
        await service.getProfile();
    } catch (error) {
        console.log('✅ Đã bắt được lỗi account không tồn tại:', error.message);
    }
    
    // Test validation errors
    try {
        const service = new ProviderProfileService();
        await service.updateProfile({
            providerName: '', // Empty name should fail validation
            phoneNumber: 'invalid-phone', // Invalid phone format
            description: 'A'.repeat(1000) // Too long description
        });
    } catch (error) {
        console.log('✅ Đã bắt được lỗi validation:', error.message);
    }
}

/**
 * Demo: Test với dữ liệu thực tế từ form
 */
function demoWithFormData() {
    console.log('📝 Demo: Test với dữ liệu form...');
    
    // Simulate form data
    const formData = {
        providerName: document.querySelector('[name="providerName"]')?.value || 'Công ty Demo',
        phoneNumber: document.querySelector('[name="phoneNumber"]')?.value || '0123456789',
        description: document.querySelector('[name="description"]')?.value || 'Mô tả demo',
        images: document.querySelector('[name="images"]')?.value || 'https://example.com/demo.jpg'
    };
    
    console.log('📋 Dữ liệu form:', formData);
    
    // Validate form data
    const errors = validateFormData(formData);
    if (Object.keys(errors).length > 0) {
        console.log('❌ Lỗi validation:', errors);
        return false;
    }
    
    console.log('✅ Dữ liệu form hợp lệ');
    return true;
}

/**
 * Validate form data
 */
function validateFormData(data) {
    const errors = {};
    
    if (!data.providerName || data.providerName.trim() === '') {
        errors.providerName = ['Tên nhà cung cấp không được để trống'];
    }
    
    if (!data.phoneNumber || data.phoneNumber.trim() === '') {
        errors.phoneNumber = ['Số điện thoại không được để trống'];
    } else if (!/^[0-9+\-\s()]+$/.test(data.phoneNumber)) {
        errors.phoneNumber = ['Số điện thoại không đúng định dạng'];
    }
    
    if (data.description && data.description.length > 500) {
        errors.description = ['Mô tả không được vượt quá 500 ký tự'];
    }
    
    if (data.images && data.images.trim() !== '') {
        try {
            new URL(data.images);
        } catch (e) {
            errors.images = ['URL hình ảnh không đúng định dạng'];
        }
    }
    
    return errors;
}

/**
 * Demo: Performance testing
 */
async function demoPerformanceTest() {
    console.log('⚡ Demo: Test hiệu suất...');
    
    const iterations = 10;
    const results = [];
    
    for (let i = 0; i < iterations; i++) {
        const startTime = performance.now();
        
        try {
            const service = new ProviderProfileService();
            await service.getProfile();
            
            const endTime = performance.now();
            const duration = endTime - startTime;
            
            results.push(duration);
            console.log(`Lần ${i + 1}: ${duration.toFixed(2)}ms`);
        } catch (error) {
            console.error(`Lần ${i + 1} thất bại:`, error);
        }
    }
    
    const avgDuration = results.reduce((a, b) => a + b, 0) / results.length;
    const minDuration = Math.min(...results);
    const maxDuration = Math.max(...results);
    
    console.log(`📊 Kết quả hiệu suất:`);
    console.log(`   - Trung bình: ${avgDuration.toFixed(2)}ms`);
    console.log(`   - Nhanh nhất: ${minDuration.toFixed(2)}ms`);
    console.log(`   - Chậm nhất: ${maxDuration.toFixed(2)}ms`);
    
    return {
        average: avgDuration,
        min: minDuration,
        max: maxDuration,
        results: results
    };
}

/**
 * Chạy tất cả demo
 */
async function runAllDemos() {
    console.log('🎬 Bắt đầu chạy tất cả demo...\n');
    
    try {
        // Demo cơ bản
        await demoGetProfile();
        await demoGetProviderId();
        await demoUpdateProfile();
        
        // Demo workflow
        await demoFullWorkflow();
        
        // Demo error handling
        await demoErrorHandling();
        
        // Demo form validation
        demoWithFormData();
        
        // Demo performance
        await demoPerformanceTest();
        
        console.log('\n🎉 Tất cả demo đã hoàn thành!');
        
    } catch (error) {
        console.error('\n💥 Demo thất bại:', error);
    }
}

// Export functions for use in other files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        demoGetProfile,
        demoUpdateProfile,
        demoGetProviderId,
        demoFullWorkflow,
        demoErrorHandling,
        demoWithFormData,
        demoPerformanceTest,
        runAllDemos,
        validateFormData
    };
}

// Auto-run demos if this file is loaded directly
if (typeof window !== 'undefined' && window.location.pathname.includes('provider-profile')) {
    console.log('🚀 Auto-running Provider Profile demos...');
    // Uncomment the line below to auto-run demos
    // runAllDemos();
}
