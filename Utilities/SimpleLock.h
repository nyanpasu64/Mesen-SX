#pragma once 
#include "stdafx.h"
#include <mutex>
#include <thread>

class SimpleLock;

class LockHandler
{
private:
	std::unique_lock<std::recursive_mutex> _lock;
public:
	LockHandler(SimpleLock *lock);
	~LockHandler();
};

class SimpleLock
{
private:
	std::recursive_mutex _mutex;

	friend class LockHandler;

public:
	SimpleLock();
	~SimpleLock();

	LockHandler AcquireSafe();

	void Acquire();
	void WaitForRelease();
	void Release();
};

